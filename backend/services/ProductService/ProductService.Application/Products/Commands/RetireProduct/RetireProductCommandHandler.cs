using MediatR;
using ProductService.Application.Common.Interfaces;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;

namespace ProductService.Application.Products.Commands.RetireProduct;

public sealed class RetireProductCommandHandler(IProductRepository repository, IEventPublisher publisher)
    : IRequestHandler<RetireProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(RetireProductCommand request, CancellationToken ct)
    {
        var product = await repository.GetByIdAsync(request.ProductId, ct)
            ?? throw new Domain.Exceptions.NotFoundException(nameof(Product), request.ProductId);

        product.Retire();

        foreach (var domainEvent in product.DomainEvents.OfType<Domain.Events.ProductRetiredDomainEvent>())
        {
            await publisher.PublishAsync(new Contracts.Events.ProductRetiredV1
            {
                EventId = Guid.NewGuid(),
                OccurredAt = DateTimeOffset.UtcNow,
                ProductId = domainEvent.ProductId,
                Sku = domainEvent.Sku
            }, ct);
        }
        product.ClearDomainEvents();

        return product.ToDto();
    }
}
