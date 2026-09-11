using OrderService.Application.Common.Messaging;
using OrderService.Domain.Common;
using OrderService.Domain.Entities;

namespace OrderService.Application.Orders.Queries.SearchOrders;

/// <summary>FR-010: search/filter orders by status and date range, with paging (SC-006).</summary>
public sealed record SearchOrdersQuery(OrderStatus? Status, DateOnly? From, DateOnly? To, int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<OrderSummaryDto>>;
