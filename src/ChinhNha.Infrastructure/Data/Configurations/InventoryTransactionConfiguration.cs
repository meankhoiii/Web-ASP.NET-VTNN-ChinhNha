using ChinhNha.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChinhNha.Infrastructure.Data.Configurations;

public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.Property(t => t.ReferenceType).HasMaxLength(50);
        builder.Property(t => t.UnitCost).HasColumnType("decimal(18,2)");
        builder.Property(t => t.Note).HasMaxLength(500);
        
        builder.HasOne(t => t.Product)
               .WithMany()
               .HasForeignKey(t => t.ProductId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.CreatedBy)
               .WithMany()
               .HasForeignKey(t => t.CreatedById)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
