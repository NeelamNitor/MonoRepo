using System.Text.Json;
using OrderService.Application.Common.Interfaces;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Infrastructure.Messaging;

/// <summary>IEventPublisher implementation that stages the event as an OutboxMessage row on the current
/// DbContext change tracker, committed atomically with the rest of the unit of work (research.md item 2).</summary>
public sealed class OutboxEventPublisher(OrderDbContext dbContext) : IEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken ct = default) where TEvent : class
    {
        var typeName = typeof(TEvent).AssemblyQualifiedName!;
        var content = JsonSerializer.Serialize(integrationEvent);
        dbContext.OutboxMessages.Add(new OutboxMessage(typeName, content));
        return Task.CompletedTask;
    }
}
