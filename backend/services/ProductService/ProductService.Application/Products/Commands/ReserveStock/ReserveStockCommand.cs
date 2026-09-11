using ProductService.Application.Common.Messaging;

namespace ProductService.Application.Products.Commands.ReserveStock;

/// <summary>FR-005: atomically reserve stock for an order line item. Called synchronously by Order Service at
/// order placement (architecture.md §4), and mirrored by the StockReservationRequestedV1 event for the async
/// path (contracts/events.md).</summary>
public sealed record ReserveStockCommand(Guid ProductId, Guid OrderId, int Quantity) : ICommand<ReserveStockResult>;

public sealed record ReserveStockResult(bool Success, Guid ProductId, string? NameSnapshot, decimal? UnitPriceSnapshot, int AvailableQuantity);
