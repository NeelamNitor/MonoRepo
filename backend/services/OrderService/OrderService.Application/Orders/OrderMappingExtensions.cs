using OrderService.Domain.Entities;

namespace OrderService.Application.Orders;

public static class OrderMappingExtensions
{
    public static OrderDto ToDto(this Order order) => new(
        order.Id,
        order.UserId,
        order.Status.ToString(),
        order.TotalAmount,
        order.LineItems.Select(li => new OrderLineItemDto(
            li.ProductId, li.ProductNameSnapshot, li.UnitPriceSnapshot, li.Quantity, li.LineTotal)).ToList(),
        order.StatusHistory
            .OrderBy(h => h.ChangedAt)
            .Select(h => new OrderStatusHistoryDto(h.PreviousStatus?.ToString(), h.NewStatus.ToString(), h.ChangedAt, h.ChangedBy))
            .ToList(),
        order.CreatedAt,
        order.UpdatedAt);

    public static OrderSummaryDto ToSummaryDto(this Order order) =>
        new(order.Id, order.Status.ToString(), order.TotalAmount, order.CreatedAt);
}
