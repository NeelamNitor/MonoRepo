namespace ProductService.Application.Common.Interfaces;

/// <summary>Publishes integration events (Contracts.Events) via the transactional outbox (research.md item 2).
/// Implemented in ProductService.Infrastructure.Messaging against MassTransit.</summary>
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken ct = default) where TEvent : class;
}
