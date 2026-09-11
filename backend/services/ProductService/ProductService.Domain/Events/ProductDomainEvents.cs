namespace ProductService.Domain.Events;

/// <summary>Domain-internal events raised by the Product aggregate. Translated to Contracts.Events integration
/// events (see ProductService.Infrastructure.Messaging) only after the originating transaction commits.</summary>
public sealed record ProductCreatedDomainEvent(Guid ProductId, string Sku);

public sealed record ProductFieldChangedDomainEvent(Guid ProductId, string Field, string? OldValue, string? NewValue, string ChangedBy);

public sealed record ProductRetiredDomainEvent(Guid ProductId, string Sku);

public sealed record StockReservedDomainEvent(Guid ProductId, Guid OrderId, int ReservedQuantity, string NameSnapshot, decimal UnitPriceSnapshot);

public sealed record StockReleasedDomainEvent(Guid ProductId, Guid OrderId, int ReleasedQuantity);
