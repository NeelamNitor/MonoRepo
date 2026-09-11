using OrderService.Domain.Common;
using OrderService.Domain.Entities;

namespace OrderService.Domain.Interfaces;

/// <summary>Repository pattern (architecture.md §3.5): persistence-ignorant contract for Order access,
/// implemented against PostgreSQL in OrderService.Infrastructure.Repositories.OrderRepository.</summary>
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task AddAsync(Order order, CancellationToken ct = default);

    /// <summary>Explicitly tracks a new OrderStatusHistory row as Added — see Order.ChangeStatus's remarks for
    /// why this can't be left to change-tracker graph-walk detection alone.</summary>
    void TrackNewStatusHistory(OrderStatusHistory history);

    Task<PagedResult<Order>> SearchAsync(
        OrderStatus? status, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct = default);
}
