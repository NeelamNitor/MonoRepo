using MediatR;
using OrderService.Domain.Interfaces;

namespace OrderService.Application.Orders.Queries.GetOrderById;

public sealed class GetOrderByIdQueryHandler(IOrderRepository repository) : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken ct)
    {
        var order = await repository.GetByIdAsync(request.OrderId, ct);
        return order?.ToDto();
    }
}
