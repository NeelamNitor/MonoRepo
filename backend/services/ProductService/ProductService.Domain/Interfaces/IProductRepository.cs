using ProductService.Domain.Common;
using ProductService.Domain.Entities;

namespace ProductService.Domain.Interfaces;

/// <summary>Repository pattern (architecture.md §3.5): persistence-ignorant contract for Product access,
/// implemented against PostgreSQL in ProductService.Infrastructure.Repositories.ProductRepository.</summary>
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default);

    Task AddAsync(Product product, CancellationToken ct = default);

    Task AddAuditAsync(ProductAudit audit, CancellationToken ct = default);

    Task<PagedResult<Product>> SearchAsync(
        string? query, ProductStatus? status, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Atomically reserves (decrements) stock via a single conditional UPDATE — see research.md item 3.
    /// Returns the reservation outcome without requiring the caller to load/save the aggregate.</summary>
    Task<StockReservationResult> ReserveStockAsync(Guid productId, Guid orderId, int quantity, CancellationToken ct = default);

    /// <summary>Atomically releases (increments) previously reserved stock, e.g. on order cancellation (FR-007).</summary>
    Task ReleaseStockAsync(Guid productId, Guid orderId, int quantity, CancellationToken ct = default);
}

public sealed record StockReservationResult(
    bool Success, Guid ProductId, int RequestedQuantity, int AvailableQuantity, string? NameSnapshot, decimal? UnitPriceSnapshot);
