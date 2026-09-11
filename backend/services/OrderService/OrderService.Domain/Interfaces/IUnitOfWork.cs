namespace OrderService.Domain.Interfaces;

/// <summary>Commits the current transaction, including any outbox messages staged for domain events raised
/// during the unit of work. Invoked by UnitOfWorkBehavior after a Command handler succeeds — never for Queries.</summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct = default);
}
