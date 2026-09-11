using ProductService.Application.Common.Messaging;

namespace ProductService.Application.Products.Commands.RetireProduct;

/// <summary>FR-002: retire a product from the sellable catalog (one-way; never a hard delete).</summary>
public sealed record RetireProductCommand(Guid ProductId) : ICommand<ProductDto>;
