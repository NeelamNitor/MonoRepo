namespace OrderService.Application.Common.Interfaces;

/// <summary>Abstraction over the narrow, synchronous calls to Product Service that PlaceOrder/Cancel need
/// (architecture.md §1 / §4): reserving stock at placement time and releasing it on cancellation. Implemented
/// in OrderService.Infrastructure.ExternalServices against Product Service's HTTP API.</summary>
public interface IProductServiceClient
{
    Task<ReserveStockResponse> ReserveStockAsync(Guid productId, Guid orderId, int quantity, CancellationToken ct = default);

    Task ReleaseStockAsync(Guid productId, Guid orderId, int quantity, CancellationToken ct = default);
}

public sealed record ReserveStockResponse(bool Success, string? NameSnapshot, decimal? UnitPriceSnapshot, int AvailableQuantity);
