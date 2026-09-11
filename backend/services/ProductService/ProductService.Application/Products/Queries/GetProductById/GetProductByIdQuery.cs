using ProductService.Application.Common.Messaging;

namespace ProductService.Application.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery(Guid ProductId) : IQuery<ProductDto?>;
