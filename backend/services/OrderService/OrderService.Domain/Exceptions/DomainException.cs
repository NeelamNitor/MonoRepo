namespace OrderService.Domain.Exceptions;

/// <summary>Raised when a domain invariant would be violated (FR-011). Mapped to HTTP 400 at the API boundary.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

/// <summary>Raised when an order status transition is not allowed (FR-006). Mapped to HTTP 409.</summary>
public class InvalidOrderStatusTransitionException : Exception
{
    public InvalidOrderStatusTransitionException(string from, string to)
        : base($"Cannot transition order from '{from}' to '{to}'.") { }
}

/// <summary>Raised when a line item's product has insufficient stock at placement time. Mapped to HTTP 409.</summary>
public class InsufficientStockException : Exception
{
    public InsufficientStockException(Guid productId, int requested, int available)
        : base($"Insufficient stock for product {productId}: requested {requested}, available {available}.") { }
}

/// <summary>Raised when an entity referenced by id does not exist. Mapped to HTTP 404 at the API boundary.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string entityName, Guid id) : base($"{entityName} '{id}' was not found.") { }
}
