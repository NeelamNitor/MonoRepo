using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductService.Domain.Entities;

namespace ProductService.Infrastructure.Persistence.Configurations;

public class ProductAuditConfiguration : IEntityTypeConfiguration<ProductAudit>
{
    public void Configure(EntityTypeBuilder<ProductAudit> builder)
    {
        builder.ToTable("product_audits");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.ProductId).HasColumnName("product_id");
        builder.Property(a => a.Field).HasColumnName("field").HasMaxLength(100);
        builder.Property(a => a.OldValue).HasColumnName("old_value");
        builder.Property(a => a.NewValue).HasColumnName("new_value");
        builder.Property(a => a.ChangedAt).HasColumnName("changed_at");
        builder.Property(a => a.ChangedBy).HasColumnName("changed_by").HasMaxLength(200);
        builder.HasIndex(a => a.ProductId);
    }
}
