using ProductService.Domain.Common;
using ProductService.Domain.Events;
using ProductService.Domain.Exceptions;

namespace ProductService.Domain.Entities;

/// <summary>A sellable catalog item. See spec.md Key Entities and FR-001/FR-002/FR-011.</summary>
public class Product : AggregateRoot
{
    public string Sku { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }
    public ProductStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Product() { } // EF Core

    public static Product Create(string sku, string name, string? description, decimal price, int stockQuantity)
    {
        if (string.IsNullOrWhiteSpace(sku)) throw new DomainException("SKU is required.");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Name is required.");
        if (price < 0) throw new DomainException("Price must not be negative.");
        if (stockQuantity < 0) throw new DomainException("Stock quantity must not be negative.");

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Sku = sku.Trim(),
            Name = name.Trim(),
            Description = description,
            Price = price,
            StockQuantity = stockQuantity,
            Status = ProductStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        product.Raise(new ProductCreatedDomainEvent(product.Id, product.Sku));
        return product;
    }

    /// <summary>Updates mutable catalog fields. Returns the field-change events so the caller can persist an
    /// audit trail (FR-001). Only non-null arguments are applied.</summary>
    public IReadOnlyCollection<ProductFieldChangedDomainEvent> UpdateDetails(
        string? name, string? description, decimal? price, int? stockQuantity, string changedBy)
    {
        var changes = new List<ProductFieldChangedDomainEvent>();

        if (name is not null && name != Name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Name must not be blank.");
            changes.Add(new ProductFieldChangedDomainEvent(Id, nameof(Name), Name, name, changedBy));
            Name = name.Trim();
        }

        if (description is not null && description != Description)
        {
            changes.Add(new ProductFieldChangedDomainEvent(Id, nameof(Description), Description, description, changedBy));
            Description = description;
        }

        if (price is not null && price != Price)
        {
            if (price < 0) throw new DomainException("Price must not be negative.");
            changes.Add(new ProductFieldChangedDomainEvent(Id, nameof(Price), Price.ToString("F2"), price.Value.ToString("F2"), changedBy));
            Price = price.Value;
        }

        if (stockQuantity is not null && stockQuantity != StockQuantity)
        {
            if (stockQuantity < 0) throw new DomainException("Stock quantity must not be negative.");
            changes.Add(new ProductFieldChangedDomainEvent(Id, nameof(StockQuantity), StockQuantity.ToString(), stockQuantity.Value.ToString(), changedBy));
            StockQuantity = stockQuantity.Value;
        }

        if (changes.Count > 0)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
            foreach (var change in changes) Raise(change);
        }

        return changes;
    }

    public void Retire()
    {
        if (Status == ProductStatus.Retired) return;
        Status = ProductStatus.Retired;
        UpdatedAt = DateTimeOffset.UtcNow;
        Raise(new ProductRetiredDomainEvent(Id, Sku));
    }

    /// <summary>In-memory reservation used only where the aggregate is already loaded (e.g. tests validating
    /// the invariant). The concurrency-safe path used by the API is ProductRepository.ReserveStockAsync, which
    /// performs a single atomic conditional UPDATE rather than a load-then-save round trip.</summary>
    public void ReserveStock(int quantity)
    {
        if (quantity <= 0) throw new DomainException("Reservation quantity must be positive.");
        if (StockQuantity < quantity) throw new InsufficientStockException(Id, quantity, StockQuantity);
        StockQuantity -= quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ReleaseStock(int quantity)
    {
        if (quantity <= 0) throw new DomainException("Release quantity must be positive.");
        StockQuantity += quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
        Raise(new StockReleasedDomainEvent(Id, Guid.Empty, quantity));
    }
}
