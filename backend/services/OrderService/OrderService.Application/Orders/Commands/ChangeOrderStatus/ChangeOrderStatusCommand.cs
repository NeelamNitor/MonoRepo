using OrderService.Application.Common.Messaging;
using OrderService.Domain.Entities;

namespace OrderService.Application.Orders.Commands.ChangeOrderStatus;

/// <summary>FR-006/FR-007/FR-008/FR-009: advance or cancel an order's status.</summary>
public sealed record ChangeOrderStatusCommand(Guid OrderId, OrderStatus NewStatus, string ChangedBy) : ICommand<OrderDto>;
