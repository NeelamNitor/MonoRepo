using ProductService.Domain.Interfaces;

namespace ProductService.Infrastructure.Persistence;

public sealed class UnitOfWork(ProductDbContext dbContext) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken ct = default) => dbContext.SaveChangesAsync(ct);
}
