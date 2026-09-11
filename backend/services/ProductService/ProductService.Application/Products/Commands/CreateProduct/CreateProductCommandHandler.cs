using MediatR;
using ProductService.Domain.Entities;
using ProductService.Domain.Exceptions;
using ProductService.Domain.Interfaces;

namespace ProductService.Application.Products.Commands.CreateProduct;

public sealed class CreateProductCommandHandler(IProductRepository repository)
    : IRequestHandler<CreateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var existing = await repository.GetBySkuAsync(request.Sku, ct);
        if (existing is not null)
        {
            throw new DomainException($"A product with SKU '{request.Sku}' already exists.");
        }

        var product = Product.Create(request.Sku, request.Name, request.Description, request.Price, request.StockQuantity);
        await repository.AddAsync(product, ct);

        return product.ToDto();
    }
}
