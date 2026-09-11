using OrderService.Domain.Interfaces;

namespace OrderService.Infrastructure.Persistence;

public sealed class UnitOfWork(OrderDbContext dbContext) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken ct = default) => dbContext.SaveChangesAsync(ct);
}
