using OrderService.Domain.Entities;

namespace OrderService.Domain.Events;

/// <summary>Domain-internal events raised by the Order aggregate. Translated to Contracts.Events integration
/// events only after the originating transaction commits (see OrderService.Infrastructure.Messaging).</summary>
public sealed record OrderStatusChangedDomainEvent(Guid OrderId, OrderStatus? PreviousStatus, OrderStatus NewStatus, string ChangedBy);

public sealed record OrderCancelledDomainEvent(Guid OrderId, IReadOnlyCollection<OrderLineItem> LineItems);
