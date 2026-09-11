namespace Contracts.Events;

public sealed record OrderLineItemRef
{
    public required Guid ProductId { get; init; }
    public required int Quantity { get; init; }
}

/// <summary>Published by Order Service when an order is cancelled — Product Service releases reserved stock.</summary>
public sealed record OrderCancelledV1
{
    public required Guid EventId { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public required Guid OrderId { get; init; }
    public required IReadOnlyCollection<OrderLineItemRef> LineItems { get; init; }
}

/// <summary>Published by Order Service on every status transition. Any interested system may subscribe.</summary>
public sealed record OrderStatusChangedV1
{
    public required Guid EventId { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public required Guid OrderId { get; init; }
    public required string? PreviousStatus { get; init; }
    public required string NewStatus { get; init; }
    public required string ChangedBy { get; init; }
}
