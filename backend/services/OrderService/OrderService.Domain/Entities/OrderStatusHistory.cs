namespace OrderService.Domain.Entities;

/// <summary>Auditable record of a single order status transition (FR-008). See data-model.md.</summary>
public class OrderStatusHistory
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public OrderStatus? PreviousStatus { get; private set; }
    public OrderStatus NewStatus { get; private set; }
    public DateTimeOffset ChangedAt { get; private set; }
    public string ChangedBy { get; private set; } = default!;

    private OrderStatusHistory() { } // EF Core

    public OrderStatusHistory(Guid orderId, OrderStatus? previousStatus, OrderStatus newStatus, string changedBy)
    {
        Id = Guid.NewGuid();
        OrderId = orderId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        ChangedBy = changedBy;
        ChangedAt = DateTimeOffset.UtcNow;
    }
}
