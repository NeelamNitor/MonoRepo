namespace Contracts.Events;

/// <summary>Requests a stock reservation for a line item of an order. See contracts/events.md.</summary>
public sealed record StockReservationRequestedV1
{
    public required Guid EventId { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public required Guid OrderId { get; init; }
    public required Guid ProductId { get; init; }
    public required int Quantity { get; init; }
}

/// <summary>Published by Product Service when a reservation succeeds.</summary>
public sealed record StockReservedV1
{
    public required Guid EventId { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public required Guid OrderId { get; init; }
    public required Guid ProductId { get; init; }
    public required int ReservedQuantity { get; init; }
    public required string NameSnapshot { get; init; }
    public required decimal UnitPriceSnapshot { get; init; }
}

/// <summary>Published by Product Service when a reservation cannot be satisfied (insufficient stock).</summary>
public sealed record StockRejectedV1
{
    public required Guid EventId { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public required Guid OrderId { get; init; }
    public required Guid ProductId { get; init; }
    public required int RequestedQuantity { get; init; }
    public required int AvailableQuantity { get; init; }
    public required string Reason { get; init; }
}
