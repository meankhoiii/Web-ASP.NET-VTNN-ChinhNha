using ChinhNha.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChinhNha.Infrastructure.Data.Configurations;

public class PurchaseOrderItemConfiguration : IEntityTypeConfiguration<PurchaseOrderItem>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderItem> builder)
    {
        builder.Property(p => p.UnitCost).HasColumnType("decimal(18,2)");
        builder.Property(p => p.TotalCost).HasColumnType("decimal(18,2)");

        builder.HasOne(poi => poi.PurchaseOrder)
               .WithMany(po => po.Items)
               .HasForeignKey(poi => poi.PurchaseOrderId)
               .OnDelete(DeleteBehavior.Cascade);
               
        builder.HasOne(poi => poi.Product)
               .WithMany()
               .HasForeignKey(poi => poi.ProductId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
