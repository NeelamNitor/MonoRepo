using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductService.Domain.Entities;

namespace ProductService.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Sku).HasColumnName("sku").HasMaxLength(50).IsRequired();
        builder.HasIndex(p => p.Sku).IsUnique();

        builder.Property(p => p.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasColumnName("description").HasMaxLength(2000);
        builder.Property(p => p.Price).HasColumnName("price").HasColumnType("decimal(18,2)");
        builder.Property(p => p.StockQuantity).HasColumnName("stock_quantity");
        builder.Property(p => p.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.CreatedAt).HasColumnName("created_at");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");

        builder.Ignore(p => p.DomainEvents);

        // PostgreSQL system column `xmin` used as an EF Core optimistic-concurrency token (shadow property) —
        // no extra column needed; defense-in-depth alongside the atomic conditional UPDATE (research.md item 3).
        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();
    }
}
