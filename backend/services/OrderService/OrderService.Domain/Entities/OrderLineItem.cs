using OrderService.Domain.Exceptions;

namespace OrderService.Domain.Entities;

/// <summary>A single product-and-quantity entry within an order. Price/name are immutable snapshots captured
/// at order time (FR-004) — later catalog changes never affect an existing line item.</summary>
public class OrderLineItem
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductNameSnapshot { get; private set; } = default!;
    public decimal UnitPriceSnapshot { get; private set; }
    public int Quantity { get; private set; }
    public decimal LineTotal => UnitPriceSnapshot * Quantity;

    private OrderLineItem() { } // EF Core

    public OrderLineItem(Guid productId, string productNameSnapshot, decimal unitPriceSnapshot, int quantity)
    {
        if (quantity < 1) throw new DomainException("Line item quantity must be at least 1.");
        if (unitPriceSnapshot < 0) throw new DomainException("Line item unit price must not be negative.");

        Id = Guid.NewGuid();
        ProductId = productId;
        ProductNameSnapshot = productNameSnapshot;
        UnitPriceSnapshot = unitPriceSnapshot;
        Quantity = quantity;
    }

    internal void AttachToOrder(Guid orderId) => OrderId = orderId;
}
