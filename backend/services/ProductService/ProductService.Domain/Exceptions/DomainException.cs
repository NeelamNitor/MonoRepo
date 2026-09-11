namespace ProductService.Domain.Exceptions;

/// <summary>Raised when a domain invariant would be violated (FR-011). Mapped to HTTP 400 at the API boundary.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

/// <summary>Raised when a stock reservation cannot be satisfied. Mapped to HTTP 409 at the API boundary.</summary>
public class InsufficientStockException : Exception
{
    public Guid ProductId { get; }
    public int Requested { get; }
    public int Available { get; }

    public InsufficientStockException(Guid productId, int requested, int available)
        : base($"Insufficient stock for product {productId}: requested {requested}, available {available}.")
    {
        ProductId = productId;
        Requested = requested;
        Available = available;
    }
}

/// <summary>Raised when an entity referenced by id does not exist. Mapped to HTTP 404 at the API boundary.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string entityName, Guid id) : base($"{entityName} '{id}' was not found.") { }
}
