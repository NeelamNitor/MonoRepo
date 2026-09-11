namespace ProductService.Domain.Entities;

/// <summary>Audit trail row for a single field change on a Product (FR-001). See data-model.md.</summary>
public class ProductAudit
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public string Field { get; private set; } = default!;
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }
    public DateTimeOffset ChangedAt { get; private set; }
    public string ChangedBy { get; private set; } = default!;

    private ProductAudit() { } // EF Core

    public ProductAudit(Guid productId, string field, string? oldValue, string? newValue, string changedBy)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        Field = field;
        OldValue = oldValue;
        NewValue = newValue;
        ChangedBy = changedBy;
        ChangedAt = DateTimeOffset.UtcNow;
    }
}
