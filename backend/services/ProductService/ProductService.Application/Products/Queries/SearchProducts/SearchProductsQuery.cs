using ProductService.Application.Common.Messaging;
using ProductService.Domain.Common;
using ProductService.Domain.Entities;

namespace ProductService.Application.Products.Queries.SearchProducts;

/// <summary>FR-010: search/filter the product catalog by name/SKU and status, with paging (SC-006).</summary>
public sealed record SearchProductsQuery(string? Query, ProductStatus? Status, int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<ProductDto>>;
