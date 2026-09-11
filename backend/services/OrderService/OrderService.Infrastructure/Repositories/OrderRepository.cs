using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Common;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Infrastructure.Repositories;

/// <summary>Repository pattern implementation (architecture.md §3.5) — the only place PostgreSQL/EF Core
/// concerns for Order are allowed to live.</summary>
public sealed class OrderRepository(OrderDbContext dbContext) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        dbContext.Orders
            .Include(o => o.LineItems)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task AddAsync(Order order, CancellationToken ct = default) =>
        await dbContext.Orders.AddAsync(order, ct);

    public void TrackNewStatusHistory(OrderStatusHistory history) =>
        dbContext.Entry(history).State = EntityState.Added;

    public async Task<PagedResult<Order>> SearchAsync(
        OrderStatus? status, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct = default)
    {
        var orders = dbContext.Orders.AsQueryable();

        if (status is not null)
        {
            orders = orders.Where(o => o.Status == status);
        }

        if (from is not null)
        {
            var fromUtc = from.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            orders = orders.Where(o => o.CreatedAt >= fromUtc);
        }

        if (to is not null)
        {
            var toUtc = to.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            orders = orders.Where(o => o.CreatedAt <= toUtc);
        }

        var totalCount = await orders.CountAsync(ct);
        var items = await orders
            .Include(o => o.LineItems)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Order>(items, totalCount, page, pageSize);
    }
}
