using FluentValidation;

namespace OrderService.Application.Orders.Commands.PlaceOrder;

public sealed class PlaceOrderValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.LineItems).NotEmpty().WithMessage("An order must contain at least one line item.");
        RuleForEach(x => x.LineItems).ChildRules(item =>
        {
            item.RuleFor(li => li.ProductId).NotEmpty();
            item.RuleFor(li => li.Quantity).GreaterThan(0);
        });
    }
}
