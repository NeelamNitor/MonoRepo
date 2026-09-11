using FluentAssertions;
using ProductService.Domain.Entities;
using ProductService.Domain.Exceptions;
using Xunit;

namespace ProductService.Tests.Domain;

// T031: unit tests for Product domain invariants (spec.md FR-011, data-model.md).
public class ProductTests
{
    [Fact]
    public void Create_WithNegativePrice_ThrowsDomainException()
    {
        var act = () => Product.Create("SKU-1", "Widget", null, -1m, 10);
        act.Should().Throw<DomainException>().WithMessage("*Price*");
    }

    [Fact]
    public void Create_WithNegativeStock_ThrowsDomainException()
    {
        var act = () => Product.Create("SKU-1", "Widget", null, 9.99m, -5);
        act.Should().Throw<DomainException>().WithMessage("*Stock*");
    }

    [Fact]
    public void Create_WithValidData_IsActiveByDefault()
    {
        var product = Product.Create("SKU-1", "Widget", "A widget", 9.99m, 10);
        product.Status.Should().Be(ProductStatus.Active);
        product.StockQuantity.Should().Be(10);
    }

    [Fact]
    public void Retire_IsOneWay()
    {
        var product = Product.Create("SKU-1", "Widget", null, 9.99m, 10);
        product.Retire();
        product.Status.Should().Be(ProductStatus.Retired);

        // Retiring again is a no-op, not an error (idempotent per RetireProductCommandHandler's usage).
        product.Retire();
        product.Status.Should().Be(ProductStatus.Retired);
    }

    [Fact]
    public void UpdateDetails_WithNegativePrice_ThrowsAndLeavesPriceUnchanged()
    {
        var product = Product.Create("SKU-1", "Widget", null, 9.99m, 10);
        var act = () => product.UpdateDetails(null, null, -1m, null, "tester");
        act.Should().Throw<DomainException>();
        product.Price.Should().Be(9.99m);
    }

    [Fact]
    public void ReserveStock_MoreThanAvailable_ThrowsInsufficientStockException()
    {
        var product = Product.Create("SKU-1", "Widget", null, 9.99m, 5);
        var act = () => product.ReserveStock(10);
        act.Should().Throw<InsufficientStockException>();
        product.StockQuantity.Should().Be(5, "a rejected reservation must not decrement stock (spec Edge Case)");
    }

    [Fact]
    public void ReserveStock_WithinAvailable_Decrements()
    {
        var product = Product.Create("SKU-1", "Widget", null, 9.99m, 5);
        product.ReserveStock(3);
        product.StockQuantity.Should().Be(2);
    }
}
