using MediatR;
using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Entities;
using OrderService.Domain.Events;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Orders.Commands.ChangeOrderStatus;

public sealed class ChangeOrderStatusCommandHandler(
    IOrderRepository repository, IEventPublisher eventPublisher, IProductServiceClient productServiceClient)
    : IRequestHandler<ChangeOrderStatusCommand, OrderDto>
{
    public async Task<OrderDto> Handle(ChangeOrderStatusCommand request, CancellationToken ct)
    {
        var order = await repository.GetByIdAsync(request.OrderId, ct)
            ?? throw new Domain.Exceptions.NotFoundException(nameof(Order), request.OrderId);

        var newHistory = order.ChangeStatus(request.NewStatus, request.ChangedBy);
        repository.TrackNewStatusHistory(newHistory);

        foreach (var domainEvent in order.DomainEvents)
        {
            switch (domainEvent)
            {
                case OrderStatusChangedDomainEvent statusChanged:
                    await eventPublisher.PublishAsync(new Contracts.Events.OrderStatusChangedV1
                    {
                        EventId = Guid.NewGuid(),
                        OccurredAt = DateTimeOffset.UtcNow,
                        OrderId = statusChanged.OrderId,
                        PreviousStatus = statusChanged.PreviousStatus?.ToString(),
                        NewStatus = statusChanged.NewStatus.ToString(),
                        ChangedBy = statusChanged.ChangedBy
                    }, ct);
                    break;

                case OrderCancelledDomainEvent cancelled:
                    var lineItemRefs = cancelled.LineItems
                        .Select(li => new Contracts.Events.OrderLineItemRef { ProductId = li.ProductId, Quantity = li.Quantity })
                        .ToList();

                    await eventPublisher.PublishAsync(new Contracts.Events.OrderCancelledV1
                    {
                        EventId = Guid.NewGuid(),
                        OccurredAt = DateTimeOffset.UtcNow,
                        OrderId = cancelled.OrderId,
                        LineItems = lineItemRefs
                    }, ct);

                    // Synchronous fallback so cancellation releases stock immediately in this demo environment
                    // (no broker provisioned — see ProductService's OrderCancelledConsumer for the event-driven
                    // path once RabbitMQ is configured, and PlaceOrderCommandHandler for the same pattern
                    // already used on the reservation side).
                    foreach (var lineItem in cancelled.LineItems)
                    {
                        await productServiceClient.ReleaseStockAsync(lineItem.ProductId, cancelled.OrderId, lineItem.Quantity, ct);
                    }
                    break;
            }
        }
        order.ClearDomainEvents();

        return order.ToDto();
    }
}
