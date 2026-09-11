using ProductService.Application.Common.Messaging;
using ProductService.Application.Products;

namespace ProductService.Application.Products.Commands.CreateProduct;

/// <summary>FR-001: create a new catalog product.</summary>
public sealed record CreateProductCommand(
    string Sku, string Name, string? Description, decimal Price, int StockQuantity) : ICommand<ProductDto>;
