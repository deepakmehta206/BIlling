using Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.Infrastructure.Persistence;

public class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("StockItems");
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Unit).HasMaxLength(8).IsRequired();
        builder.Property(x => x.OnHand).HasPrecision(12, 3);
        builder.Property(x => x.ReorderLevel).HasPrecision(12, 3);
        builder.HasIndex(x => new { x.RestaurantId, x.Name }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasOne(x => x.Restaurant).WithMany().HasForeignKey(x => x.RestaurantId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RecipeLineConfiguration : IEntityTypeConfiguration<RecipeLine>
{
    public void Configure(EntityTypeBuilder<RecipeLine> builder)
    {
        builder.ToTable("RecipeLines");
        builder.Property(x => x.Quantity).HasPrecision(12, 3);
        builder.HasIndex(x => new { x.MenuItemId, x.StockItemId }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasOne(x => x.MenuItem).WithMany().HasForeignKey(x => x.MenuItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.StockItem).WithMany(x => x.RecipeLines).HasForeignKey(x => x.StockItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements");
        builder.Property(x => x.Quantity).HasPrecision(12, 3);
        builder.Property(x => x.UnitCost).HasPrecision(10, 2);
        builder.Property(x => x.Reason).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(200);
        builder.HasOne(x => x.StockItem).WithMany(x => x.Movements).HasForeignKey(x => x.StockItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Bill).WithMany().HasForeignKey(x => x.BillId).OnDelete(DeleteBehavior.Restrict);
    }
}
