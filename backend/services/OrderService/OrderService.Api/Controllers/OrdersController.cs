using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Common.Interfaces;
using OrderService.Application.Orders.Commands.ChangeOrderStatus;
using OrderService.Application.Orders.Commands.PlaceOrder;
using OrderService.Application.Orders.Queries.GetOrderById;
using OrderService.Application.Orders.Queries.SearchOrders;
using OrderService.Domain.Entities;

namespace OrderService.Api.Controllers;

/// <summary>Implements contracts/order-service.openapi.yaml. Thin: builds a Command/Query and sends it via
/// MediatR (architecture.md §3.3) — no business logic lives here.</summary>
[ApiController]
[Route("order-service/v1/orders")]
[Authorize]
public sealed class OrdersController(ISender mediator, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] OrderStatus? status, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await mediator.Send(new SearchOrdersQuery(status, from, to, page, pageSize));
        return Ok(new { items = result.Items, totalCount = result.TotalCount });
    }

    [HttpGet("{orderId:guid}")]
    public async Task<IActionResult> GetById(Guid orderId)
    {
        var order = await mediator.Send(new GetOrderByIdQuery(orderId));
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost]
    public async Task<IActionResult> Place([FromBody] PlaceOrderRequest request)
    {
        var lineItems = request.LineItems
            .Select(li => new PlaceOrderLineItemRequest(li.ProductId, li.Quantity))
            .ToList();

        var result = await mediator.Send(new PlaceOrderCommand(currentUser.UserId, lineItems));
        return CreatedAtAction(nameof(GetById), new { orderId = result.Id }, result);
    }

    [HttpPatch("{orderId:guid}/status")]
    [Authorize(Roles = "operations")]
    public async Task<IActionResult> ChangeStatus(Guid orderId, [FromBody] ChangeOrderStatusRequest request)
    {
        var result = await mediator.Send(new ChangeOrderStatusCommand(orderId, request.NewStatus, currentUser.UserId));
        return Ok(result);
    }
}

public sealed record PlaceOrderLineItemRequestDto(Guid ProductId, int Quantity);
public sealed record PlaceOrderRequest(IReadOnlyCollection<PlaceOrderLineItemRequestDto> LineItems);
public sealed record ChangeOrderStatusRequest(OrderStatus NewStatus);
