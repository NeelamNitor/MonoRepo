using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.UserId).HasColumnName("user_id").HasMaxLength(200).IsRequired();
        builder.Property(o => o.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.CreatedAt).HasColumnName("created_at");
        builder.Property(o => o.UpdatedAt).HasColumnName("updated_at");

        builder.Ignore(o => o.DomainEvents);
        builder.Ignore(o => o.TotalAmount); // computed from line items, not persisted

        builder.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();

        builder.HasMany(o => o.LineItems)
            .WithOne()
            .HasForeignKey(li => li.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(o => o.LineItems).UsePropertyAccessMode(PropertyAccessMode.Field).HasField("_lineItems");

        builder.HasMany(o => o.StatusHistory)
            .WithOne()
            .HasForeignKey(h => h.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(o => o.StatusHistory).UsePropertyAccessMode(PropertyAccessMode.Field).HasField("_statusHistory");
    }
}

public class OrderLineItemConfiguration : IEntityTypeConfiguration<OrderLineItem>
{
    public void Configure(EntityTypeBuilder<OrderLineItem> builder)
    {
        builder.ToTable("order_line_items");
        builder.HasKey(li => li.Id);
        builder.Property(li => li.OrderId).HasColumnName("order_id");
        builder.Property(li => li.ProductId).HasColumnName("product_id");
        builder.Property(li => li.ProductNameSnapshot).HasColumnName("product_name_snapshot").HasMaxLength(200);
        builder.Property(li => li.UnitPriceSnapshot).HasColumnName("unit_price_snapshot").HasColumnType("decimal(18,2)");
        builder.Property(li => li.Quantity).HasColumnName("quantity");
        builder.Ignore(li => li.LineTotal); // computed
    }
}

public class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.ToTable("order_status_history");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.OrderId).HasColumnName("order_id");
        builder.Property(h => h.PreviousStatus).HasColumnName("previous_status").HasConversion<string>().HasMaxLength(20);
        builder.Property(h => h.NewStatus).HasColumnName("new_status").HasConversion<string>().HasMaxLength(20);
        builder.Property(h => h.ChangedAt).HasColumnName("changed_at");
        builder.Property(h => h.ChangedBy).HasColumnName("changed_by").HasMaxLength(200);
    }
}
