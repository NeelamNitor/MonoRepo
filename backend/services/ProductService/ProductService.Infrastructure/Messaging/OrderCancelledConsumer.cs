using Contracts.Events;
using MassTransit;
using MediatR;
using ProductService.Application.Products.Commands.ReleaseStock;

namespace ProductService.Infrastructure.Messaging;

/// <summary>Consumes OrderCancelledV1 and releases the reserved stock for each line item (FR-007).
/// Only reachable when a real broker (RabbitMQ) is configured — see DependencyInjection.AddProductMessaging.
/// The synchronous HTTP fallback used by the demo stack without a broker lives in OrderService's
/// CancelOrderStockReleaseClient, mirroring the same synchronous pattern already used for reservation.</summary>
public sealed class OrderCancelledConsumer(ISender mediator) : IConsumer<OrderCancelledV1>
{
    public async Task Consume(ConsumeContext<OrderCancelledV1> context)
    {
        foreach (var lineItem in context.Message.LineItems)
        {
            await mediator.Send(new ReleaseStockCommand(lineItem.ProductId, context.Message.OrderId, lineItem.Quantity));
        }
    }
}
