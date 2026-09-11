using ProductService.Domain.Entities;

namespace ProductService.Application.Products;

public static class ProductMappingExtensions
{
    public static ProductDto ToDto(this Product product) => new(
        product.Id,
        product.Sku,
        product.Name,
        product.Description,
        product.Price,
        product.StockQuantity,
        product.Status.ToString(),
        product.CreatedAt,
        product.UpdatedAt);
}
