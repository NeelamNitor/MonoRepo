namespace OrderService.Application.Orders;

public sealed record OrderLineItemDto(Guid ProductId, string ProductNameSnapshot, decimal UnitPriceSnapshot, int Quantity, decimal LineTotal);

public sealed record OrderStatusHistoryDto(string? PreviousStatus, string NewStatus, DateTimeOffset ChangedAt, string ChangedBy);

public sealed record OrderDto(
    Guid Id,
    string UserId,
    string Status,
    decimal TotalAmount,
    IReadOnlyCollection<OrderLineItemDto> LineItems,
    IReadOnlyCollection<OrderStatusHistoryDto> StatusHistory,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record OrderSummaryDto(Guid Id, string Status, decimal TotalAmount, DateTimeOffset CreatedAt);
