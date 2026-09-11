using ProductService.Application.Common.Messaging;

namespace ProductService.Application.Products.Commands.UpdateProduct;

/// <summary>FR-001: update mutable product fields. Null fields are left unchanged.</summary>
public sealed record UpdateProductCommand(
    Guid ProductId, string? Name, string? Description, decimal? Price, int? StockQuantity, string ChangedBy)
    : ICommand<ProductDto>;
