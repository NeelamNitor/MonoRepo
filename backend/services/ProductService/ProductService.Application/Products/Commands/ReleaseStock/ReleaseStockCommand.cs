using MediatR;
using ProductService.Application.Common.Messaging;

namespace ProductService.Application.Products.Commands.ReleaseStock;

/// <summary>FR-007: release previously reserved stock, e.g. when an order is cancelled. Triggered by consuming
/// OrderCancelledV1 (contracts/events.md) — see ProductService.Infrastructure.Messaging.</summary>
public sealed record ReleaseStockCommand(Guid ProductId, Guid OrderId, int Quantity) : ICommand<Unit>;
