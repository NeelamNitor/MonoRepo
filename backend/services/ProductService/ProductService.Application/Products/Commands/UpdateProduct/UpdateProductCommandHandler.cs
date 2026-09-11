using MediatR;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;

namespace ProductService.Application.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandHandler(IProductRepository repository)
    : IRequestHandler<UpdateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        var product = await repository.GetByIdAsync(request.ProductId, ct)
            ?? throw new Domain.Exceptions.NotFoundException(nameof(Product), request.ProductId);

        var changes = product.UpdateDetails(request.Name, request.Description, request.Price, request.StockQuantity, request.ChangedBy);

        foreach (var change in changes)
        {
            await repository.AddAuditAsync(
                new ProductAudit(change.ProductId, change.Field, change.OldValue, change.NewValue, change.ChangedBy), ct);
        }

        return product.ToDto();
    }
}
