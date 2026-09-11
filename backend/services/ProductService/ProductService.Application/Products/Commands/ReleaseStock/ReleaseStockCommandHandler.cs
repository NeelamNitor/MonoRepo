using MediatR;
using ProductService.Domain.Interfaces;

namespace ProductService.Application.Products.Commands.ReleaseStock;

public sealed class ReleaseStockCommandHandler(IProductRepository repository)
    : IRequestHandler<ReleaseStockCommand, Unit>
{
    public async Task<Unit> Handle(ReleaseStockCommand request, CancellationToken ct)
    {
        await repository.ReleaseStockAsync(request.ProductId, request.OrderId, request.Quantity, ct);
        return Unit.Value;
    }
}
