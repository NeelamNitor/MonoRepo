using OrderService.Application.Common.Messaging;

namespace OrderService.Application.Orders.Queries.GetOrderById;

public sealed record GetOrderByIdQuery(Guid OrderId) : IQuery<OrderDto?>;
