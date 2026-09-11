using MediatR;
using ProductService.Application.Common.Interfaces;
using ProductService.Domain.Interfaces;

namespace ProductService.Application.Products.Commands.ReserveStock;

public sealed class ReserveStockCommandHandler(IProductRepository repository, IEventPublisher publisher)
    : IRequestHandler<ReserveStockCommand, ReserveStockResult>
{
    public async Task<ReserveStockResult> Handle(ReserveStockCommand request, CancellationToken ct)
    {
        var result = await repository.ReserveStockAsync(request.ProductId, request.OrderId, request.Quantity, ct);

        if (result.Success)
        {
            await publisher.PublishAsync(new Contracts.Events.StockReservedV1
            {
                EventId = Guid.NewGuid(),
                OccurredAt = DateTimeOffset.UtcNow,
                OrderId = request.OrderId,
                ProductId = request.ProductId,
                ReservedQuantity = request.Quantity,
                NameSnapshot = result.NameSnapshot!,
                UnitPriceSnapshot = result.UnitPriceSnapshot!.Value
            }, ct);
        }
        else
        {
            await publisher.PublishAsync(new Contracts.Events.StockRejectedV1
            {
                EventId = Guid.NewGuid(),
                OccurredAt = DateTimeOffset.UtcNow,
                OrderId = request.OrderId,
                ProductId = request.ProductId,
                RequestedQuantity = request.Quantity,
                AvailableQuantity = result.AvailableQuantity,
                Reason = "InsufficientStock"
            }, ct);
        }

        return new ReserveStockResult(result.Success, result.ProductId, result.NameSnapshot, result.UnitPriceSnapshot, result.AvailableQuantity);
    }
}
