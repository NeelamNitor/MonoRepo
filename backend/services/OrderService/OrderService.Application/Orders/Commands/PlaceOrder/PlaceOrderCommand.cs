using OrderService.Application.Common.Messaging;

namespace OrderService.Application.Orders.Commands.PlaceOrder;

public sealed record PlaceOrderLineItemRequest(Guid ProductId, int Quantity);

/// <summary>FR-003/FR-004/FR-005: place an order against the catalog with price/stock captured atomically at
/// placement time.</summary>
public sealed record PlaceOrderCommand(string UserId, IReadOnlyCollection<PlaceOrderLineItemRequest> LineItems)
    : ICommand<OrderDto>;
