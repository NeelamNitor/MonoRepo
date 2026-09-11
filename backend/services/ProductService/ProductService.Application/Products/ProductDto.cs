namespace ProductService.Application.Products;

public sealed record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
