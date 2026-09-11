using FluentAssertions;
using OrderService.Domain.Entities;
using OrderService.Domain.Exceptions;
using Xunit;

namespace OrderService.Tests.Domain;

// T068: unit tests for the order status state machine (data-model.md, spec.md FR-006).
public class OrderStatusTransitionTests
{
    private static Order NewPlacedOrder() =>
        Order.Place(Guid.NewGuid(), "user-1", new[] { new OrderLineItem(Guid.NewGuid(), "Widget", 9.99m, 1) });

    [Theory]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Cancelled)]
    public void FromPlaced_AllowedTransitions_Succeed(OrderStatus target)
    {
        var order = NewPlacedOrder();
        order.ChangeStatus(target, "ops-1");
        order.Status.Should().Be(target);
    }

    [Theory]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    public void FromPlaced_DisallowedTransitions_Throw(OrderStatus target)
    {
        var order = NewPlacedOrder();
        var act = () => order.ChangeStatus(target, "ops-1");
        act.Should().Throw<InvalidOrderStatusTransitionException>();
        order.Status.Should().Be(OrderStatus.Placed, "a rejected transition must not change state");
    }

    [Fact]
    public void FullLifecycle_PlacedToDelivered_Succeeds()
    {
        var order = NewPlacedOrder();
        order.ChangeStatus(OrderStatus.Confirmed, "ops-1");
        order.ChangeStatus(OrderStatus.Shipped, "ops-1");
        order.ChangeStatus(OrderStatus.Delivered, "ops-1");

        order.Status.Should().Be(OrderStatus.Delivered);
        order.StatusHistory.Should().HaveCount(4, "Placed + 3 transitions");
    }

    [Theory]
    [InlineData(OrderStatus.Placed)]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Cancelled)]
    public void FromDelivered_IsTerminal_AllTransitionsThrow(OrderStatus target)
    {
        var order = NewPlacedOrder();
        order.ChangeStatus(OrderStatus.Confirmed, "ops-1");
        order.ChangeStatus(OrderStatus.Shipped, "ops-1");
        order.ChangeStatus(OrderStatus.Delivered, "ops-1");

        var act = () => order.ChangeStatus(target, "ops-1");
        act.Should().Throw<InvalidOrderStatusTransitionException>();
    }

    [Fact]
    public void FromCancelled_IsTerminal()
    {
        var order = NewPlacedOrder();
        order.ChangeStatus(OrderStatus.Cancelled, "ops-1");

        var act = () => order.ChangeStatus(OrderStatus.Confirmed, "ops-1");
        act.Should().Throw<InvalidOrderStatusTransitionException>();
    }

    [Fact]
    public void Place_WithNoLineItems_ThrowsDomainException()
    {
        var act = () => Order.Place(Guid.NewGuid(), "user-1", Array.Empty<OrderLineItem>());
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void TotalAmount_SumsLineTotals()
    {
        var order = Order.Place(Guid.NewGuid(), "user-1", new[]
        {
            new OrderLineItem(Guid.NewGuid(), "Widget", 10m, 2),
            new OrderLineItem(Guid.NewGuid(), "Gadget", 5m, 3),
        });

        order.TotalAmount.Should().Be(35m); // (10*2) + (5*3)
    }
}
