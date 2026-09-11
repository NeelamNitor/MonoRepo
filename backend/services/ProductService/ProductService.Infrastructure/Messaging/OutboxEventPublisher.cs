using System.Text.Json;
using ProductService.Application.Common.Interfaces;
using ProductService.Infrastructure.Persistence;

namespace ProductService.Infrastructure.Messaging;

/// <summary>IEventPublisher implementation that stages the event as an OutboxMessage row on the current
/// DbContext change tracker. The row is committed atomically with the rest of the unit of work when
/// UnitOfWorkBehavior calls SaveChangesAsync — the event is never published without the DB change, and never
/// lost after a crash post-commit (research.md item 2).</summary>
public sealed class OutboxEventPublisher(ProductDbContext dbContext) : IEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken ct = default) where TEvent : class
    {
        var typeName = typeof(TEvent).AssemblyQualifiedName!;
        var content = JsonSerializer.Serialize(integrationEvent);
        dbContext.OutboxMessages.Add(new OutboxMessage(typeName, content));
        return Task.CompletedTask;
    }
}
