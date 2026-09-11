using MediatR;
using OrderService.Domain.Common;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Orders.Queries.SearchOrders;

public sealed class SearchOrdersQueryHandler(IOrderRepository repository)
    : IRequestHandler<SearchOrdersQuery, PagedResult<OrderSummaryDto>>
{
    public async Task<PagedResult<OrderSummaryDto>> Handle(SearchOrdersQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var result = await repository.SearchAsync(request.Status, request.From, request.To, page, pageSize, ct);

        return new PagedResult<OrderSummaryDto>(
            result.Items.Select(o => o.ToSummaryDto()).ToList(), result.TotalCount, result.Page, result.PageSize);
    }
}
