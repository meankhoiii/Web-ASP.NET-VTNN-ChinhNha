using ChinhNha.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChinhNha.Infrastructure.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property(o => o.OrderCode).IsRequired().HasMaxLength(20);
        builder.HasIndex(o => o.OrderCode).IsUnique();

        builder.Property(o => o.SubTotal).HasColumnType("decimal(18,2)");
        builder.Property(o => o.ShippingFee).HasColumnType("decimal(18,2)");
        builder.Property(o => o.Discount).HasColumnType("decimal(18,2)");
        builder.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");

        builder.Property(o => o.ReceiverName).IsRequired().HasMaxLength(200);
        builder.Property(o => o.ReceiverPhone).IsRequired().HasMaxLength(20);
        builder.Property(o => o.ReceiverEmail).HasMaxLength(255);
        builder.Property(o => o.ShippingProvince).IsRequired().HasMaxLength(100);
        builder.Property(o => o.ShippingDistrict).IsRequired().HasMaxLength(100);
        builder.Property(o => o.ShippingWard).IsRequired().HasMaxLength(100);
        builder.Property(o => o.ShippingAddress).IsRequired().HasMaxLength(500);
        
        builder.Property(o => o.Note).HasMaxLength(1000);
        builder.Property(o => o.CancelReason).HasMaxLength(500);

        builder.HasOne(o => o.User)
               .WithMany(u => u.Orders)
               .HasForeignKey(o => o.UserId)
             .IsRequired(false)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
