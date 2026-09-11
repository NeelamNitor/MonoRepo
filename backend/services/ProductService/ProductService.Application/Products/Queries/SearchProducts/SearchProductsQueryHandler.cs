using MediatR;
using ProductService.Domain.Common;
using ProductService.Domain.Interfaces;

namespace ProductService.Application.Products.Queries.SearchProducts;

public sealed class SearchProductsQueryHandler(IProductRepository repository)
    : IRequestHandler<SearchProductsQuery, PagedResult<ProductDto>>
{
    public async Task<PagedResult<ProductDto>> Handle(SearchProductsQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var result = await repository.SearchAsync(request.Query, request.Status, page, pageSize, ct);

        return new PagedResult<ProductDto>(
            result.Items.Select(p => p.ToDto()).ToList(), result.TotalCount, result.Page, result.PageSize);
    }
}
