using MediatR;
using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Entities;
using OrderService.Domain.Exceptions;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Orders.Commands.PlaceOrder;

/// <summary>Implements the request flow documented in architecture.md §4: reserve stock synchronously per
/// line item (research.md item 3's atomic conditional UPDATE, called through Product Service's API), build
/// price/name snapshots from the reservation response (FR-004), then persist the Order aggregate.</summary>
public sealed class PlaceOrderCommandHandler(
    IOrderRepository repository, IProductServiceClient productServiceClient, IEventPublisher eventPublisher)
    : IRequestHandler<PlaceOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(PlaceOrderCommand request, CancellationToken ct)
    {
        var orderId = Guid.NewGuid();
        var reservedSoFar = new List<(Guid ProductId, int Quantity)>();
        var lineItems = new List<OrderLineItem>();

        try
        {
            foreach (var line in request.LineItems)
            {
                var reservation = await productServiceClient.ReserveStockAsync(line.ProductId, orderId, line.Quantity, ct);

                if (!reservation.Success)
                {
                    throw new InsufficientStockException(line.ProductId, line.Quantity, reservation.AvailableQuantity);
                }

                reservedSoFar.Add((line.ProductId, line.Quantity));
                lineItems.Add(new OrderLineItem(line.ProductId, reservation.NameSnapshot!, reservation.UnitPriceSnapshot!.Value, line.Quantity));
            }
        }
        catch
        {
            // Spec Edge Case: an order that can't be fully satisfied must decrement no stock at all — release
            // every reservation already made for this attempt before propagating the failure.
            foreach (var (productId, quantity) in reservedSoFar)
            {
                await productServiceClient.ReleaseStockAsync(productId, orderId, quantity, ct);
            }
            throw;
        }

        var order = Order.Place(orderId, request.UserId, lineItems);
        await repository.AddAsync(order, ct);

        foreach (var domainEvent in order.DomainEvents.OfType<Domain.Events.OrderStatusChangedDomainEvent>())
        {
            await eventPublisher.PublishAsync(new Contracts.Events.OrderStatusChangedV1
            {
                EventId = Guid.NewGuid(),
                OccurredAt = DateTimeOffset.UtcNow,
                OrderId = domainEvent.OrderId,
                PreviousStatus = domainEvent.PreviousStatus?.ToString(),
                NewStatus = domainEvent.NewStatus.ToString(),
                ChangedBy = domainEvent.ChangedBy
            }, ct);
        }
        order.ClearDomainEvents();

        return order.ToDto();
    }
}
