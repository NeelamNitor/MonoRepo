using Microsoft.EntityFrameworkCore;
using ProductService.Domain.Common;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using ProductService.Infrastructure.Persistence;

namespace ProductService.Infrastructure.Repositories;

/// <summary>Repository pattern implementation (architecture.md §3.5) — the only place PostgreSQL/EF Core
/// concerns for Product are allowed to live.</summary>
public sealed class ProductRepository(ProductDbContext dbContext) : IProductRepository
{
    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        dbContext.Products.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default) =>
        dbContext.Products.FirstOrDefaultAsync(p => p.Sku == sku, ct);

    public async Task AddAsync(Product product, CancellationToken ct = default) =>
        await dbContext.Products.AddAsync(product, ct);

    public async Task AddAuditAsync(ProductAudit audit, CancellationToken ct = default) =>
        await dbContext.ProductAudits.AddAsync(audit, ct);

    public async Task<PagedResult<Product>> SearchAsync(
        string? query, ProductStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        var products = dbContext.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var pattern = $"%{query.Trim()}%";
            products = products.Where(p => EF.Functions.ILike(p.Name, pattern) || EF.Functions.ILike(p.Sku, pattern));
        }

        if (status is not null)
        {
            products = products.Where(p => p.Status == status);
        }

        var totalCount = await products.CountAsync(ct);
        var items = await products
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Product>(items, totalCount, page, pageSize);
    }

    /// <summary>Atomic conditional UPDATE (research.md item 3) — a single statement, so it is correct under
    /// arbitrary concurrent callers without an explicit lock: `UPDATE products SET stock = stock - @qty
    /// WHERE id = @id AND stock >= @qty`. EF Core 8's ExecuteUpdateAsync compiles to exactly this shape.</summary>
    public async Task<StockReservationResult> ReserveStockAsync(Guid productId, Guid orderId, int quantity, CancellationToken ct = default)
    {
        var rowsAffected = await dbContext.Products
            .Where(p => p.Id == productId && p.StockQuantity >= quantity && p.Status == ProductStatus.Active)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.StockQuantity, p => p.StockQuantity - quantity)
                .SetProperty(p => p.UpdatedAt, DateTimeOffset.UtcNow), ct);

        // Re-read for the response payload (name/price snapshot, or current availability on failure).
        var product = await dbContext.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId, ct);
        if (product is null)
        {
            return new StockReservationResult(false, productId, quantity, 0, null, null);
        }

        return rowsAffected > 0
            ? new StockReservationResult(true, productId, quantity, product.StockQuantity, product.Name, product.Price)
            : new StockReservationResult(false, productId, quantity, product.StockQuantity, null, null);
    }

    public async Task ReleaseStockAsync(Guid productId, Guid orderId, int quantity, CancellationToken ct = default)
    {
        await dbContext.Products
            .Where(p => p.Id == productId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.StockQuantity, p => p.StockQuantity + quantity)
                .SetProperty(p => p.UpdatedAt, DateTimeOffset.UtcNow), ct);
    }
}
