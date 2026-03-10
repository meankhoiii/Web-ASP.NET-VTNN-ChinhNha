using ChinhNha.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChinhNha.Infrastructure.Data.Configurations;

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.Property(v => v.VariantName).IsRequired().HasMaxLength(200);
        
        builder.Property(v => v.SKU).HasMaxLength(50);
        builder.HasIndex(v => v.SKU).IsUnique().HasFilter("[SKU] IS NOT NULL");

        builder.Property(v => v.Price).HasColumnType("decimal(18,2)");
        builder.Property(v => v.SalePrice).HasColumnType("decimal(18,2)");
        builder.Property(v => v.Weight).HasColumnType("decimal(10,2)");

        builder.HasOne(v => v.Product)
               .WithMany(p => p.Variants)
               .HasForeignKey(v => v.ProductId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
