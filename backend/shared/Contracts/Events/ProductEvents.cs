namespace Contracts.Events;

/// <summary>Published by Product Service when a product is retired. No required v1 consumer — see contracts/events.md.</summary>
public sealed record ProductRetiredV1
{
    public required Guid EventId { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public required Guid ProductId { get; init; }
    public required string Sku { get; init; }
}
