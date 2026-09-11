using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductService.Application.Common.Interfaces;
using ProductService.Application.Products.Commands.CreateProduct;
using ProductService.Application.Products.Commands.ReleaseStock;
using ProductService.Application.Products.Commands.ReserveStock;
using ProductService.Application.Products.Commands.RetireProduct;
using ProductService.Application.Products.Commands.UpdateProduct;
using ProductService.Application.Products.Queries.GetProductById;
using ProductService.Application.Products.Queries.SearchProducts;
using ProductService.Domain.Entities;

namespace ProductService.Api.Controllers;

/// <summary>Implements contracts/product-service.openapi.yaml. Thin: builds a Command/Query and sends it via
/// MediatR (architecture.md §3.3) — no business logic lives here.</summary>
[ApiController]
[Route("product-service/v1/products")]
public sealed class ProductsController(ISender mediator, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Search(
        [FromQuery] string? q, [FromQuery] ProductStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await mediator.Send(new SearchProductsQuery(q, status, page, pageSize));
        return Ok(new { items = result.Items, totalCount = result.TotalCount });
    }

    [HttpGet("{productId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid productId)
    {
        var product = await mediator.Send(new GetProductByIdQuery(productId));
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    [Authorize(Roles = "catalog-manager")]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        var result = await mediator.Send(new CreateProductCommand(
            request.Sku, request.Name, request.Description, request.Price, request.StockQuantity));
        return CreatedAtAction(nameof(GetById), new { productId = result.Id }, result);
    }

    [HttpPatch("{productId:guid}")]
    [Authorize(Roles = "catalog-manager")]
    public async Task<IActionResult> Update(Guid productId, [FromBody] UpdateProductRequest request)
    {
        var result = await mediator.Send(new UpdateProductCommand(
            productId, request.Name, request.Description, request.Price, request.StockQuantity, currentUser.UserId));
        return Ok(result);
    }

    [HttpPost("{productId:guid}/retire")]
    [Authorize(Roles = "catalog-manager")]
    public async Task<IActionResult> Retire(Guid productId)
    {
        var result = await mediator.Send(new RetireProductCommand(productId));
        return Ok(result);
    }

    /// <summary>Called synchronously by Order Service at order placement (architecture.md §4) and reachable by
    /// any authenticated service identity — see contracts/product-service.openapi.yaml.</summary>
    [HttpPost("{productId:guid}/reserve-stock")]
    [Authorize]
    public async Task<IActionResult> ReserveStock(Guid productId, [FromBody] ReserveStockRequest request)
    {
        var result = await mediator.Send(new ReserveStockCommand(productId, request.OrderId, request.Quantity));
        if (!result.Success)
        {
            return Conflict(new { productId, availableQuantity = result.AvailableQuantity });
        }

        return Ok(new
        {
            productId,
            nameSnapshot = result.NameSnapshot,
            unitPriceSnapshot = result.UnitPriceSnapshot,
            reservedQuantity = request.Quantity
        });
    }
    /// <summary>Synchronous fallback used by Order Service when cancelling an order, mirroring the same
    /// synchronous-call pattern already used for reservation (architecture.md §1) — kept alongside the
    /// OrderCancelledV1 consumer (Messaging/OrderCancelledConsumer.cs) so cancellation works end-to-end even
    /// without a broker provisioned (no Docker/RabbitMQ in this environment; see docker-compose.yml).</summary>
    [HttpPost("{productId:guid}/release-stock")]
    [Authorize]
    public async Task<IActionResult> ReleaseStock(Guid productId, [FromBody] ReleaseStockRequest request)
    {
        await mediator.Send(new ReleaseStockCommand(productId, request.OrderId, request.Quantity));
        return NoContent();
    }
}

public sealed record CreateProductRequest(string Sku, string Name, string? Description, decimal Price, int StockQuantity);
public sealed record UpdateProductRequest(string? Name, string? Description, decimal? Price, int? StockQuantity);
public sealed record ReserveStockRequest(Guid OrderId, int Quantity);
public sealed record ReleaseStockRequest(Guid OrderId, int Quantity);
