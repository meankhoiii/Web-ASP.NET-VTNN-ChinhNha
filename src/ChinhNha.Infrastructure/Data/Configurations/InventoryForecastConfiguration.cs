using ChinhNha.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChinhNha.Infrastructure.Data.Configurations;

public class InventoryForecastConfiguration : IEntityTypeConfiguration<InventoryForecast>
{
    public void Configure(EntityTypeBuilder<InventoryForecast> builder)
    {
        builder.Property(f => f.PredictedDemand).HasColumnType("decimal(10,2)");
        builder.Property(f => f.ConfidenceLower).HasColumnType("decimal(10,2)");
        builder.Property(f => f.ConfidenceUpper).HasColumnType("decimal(10,2)");
        builder.Property(f => f.ActualDemand).HasColumnType("decimal(10,2)");
        builder.Property(f => f.MAPE).HasColumnType("decimal(5,2)");
        builder.Property(f => f.ModelVersion).HasMaxLength(50);

        builder.HasOne(f => f.Product)
               .WithMany()
               .HasForeignKey(f => f.ProductId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
