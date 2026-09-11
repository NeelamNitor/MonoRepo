namespace OrderService.Domain.Entities;

/// <summary>See data-model.md's state machine: Placed -> Confirmed -> Shipped -> Delivered, or
/// Placed/Confirmed -> Cancelled. Delivered and Cancelled are terminal (FR-006).</summary>
public enum OrderStatus
{
    Placed = 0,
    Confirmed = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4
}
