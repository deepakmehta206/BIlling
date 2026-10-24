using Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.Infrastructure.Persistence;

public class MenuCategoryConfiguration : IEntityTypeConfiguration<MenuCategory>
{
    public void Configure(EntityTypeBuilder<MenuCategory> builder)
    {
        builder.ToTable("MenuCategories");
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.HasIndex(x => new { x.RestaurantId, x.Name }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasOne(x => x.Restaurant).WithMany().HasForeignKey(x => x.RestaurantId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("MenuItems");
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(400);
        builder.Property(x => x.FoodType).HasMaxLength(16).IsRequired();
        builder.Property(x => x.Price).HasPrecision(10, 2);
        builder.HasOne(x => x.Category).WithMany(x => x.Items).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ItemVariantConfiguration : IEntityTypeConfiguration<ItemVariant>
{
    public void Configure(EntityTypeBuilder<ItemVariant> builder)
    {
        builder.ToTable("ItemVariants");
        builder.Property(x => x.Name).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Price).HasPrecision(10, 2);
        builder.HasOne(x => x.MenuItem).WithMany(x => x.Variants).HasForeignKey(x => x.MenuItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ItemAddonConfiguration : IEntityTypeConfiguration<ItemAddon>
{
    public void Configure(EntityTypeBuilder<ItemAddon> builder)
    {
        builder.ToTable("ItemAddons");
        builder.Property(x => x.Name).HasMaxLength(60).IsRequired();
        builder.Property(x => x.Price).HasPrecision(10, 2);
        builder.HasOne(x => x.MenuItem).WithMany(x => x.Addons).HasForeignKey(x => x.MenuItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class DiningTableConfiguration : IEntityTypeConfiguration<DiningTable>
{
    public void Configure(EntityTypeBuilder<DiningTable> builder)
    {
        builder.ToTable("DiningTables");
        builder.Property(x => x.Name).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Section).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.QrToken).HasMaxLength(16).IsRequired();
        builder.Property(x => x.PosX).HasPrecision(5, 2);
        builder.Property(x => x.PosY).HasPrecision(5, 2);
        builder.HasIndex(x => x.QrToken).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => new { x.RestaurantId, x.Name }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasOne(x => x.Restaurant).WithMany().HasForeignKey(x => x.RestaurantId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class TableBookingConfiguration : IEntityTypeConfiguration<TableBooking>
{
    public void Configure(EntityTypeBuilder<TableBooking> builder)
    {
        builder.ToTable("TableBookings");
        builder.Property(x => x.TableName).HasMaxLength(40).IsRequired();
        builder.Property(x => x.GuestName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.GuestPhone).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(240);
        builder.HasIndex(x => new { x.DiningTableId, x.StartsAt });
        builder.HasOne(x => x.DiningTable).WithMany(x => x.Bookings).HasForeignKey(x => x.DiningTableId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class GuestOrderConfiguration : IEntityTypeConfiguration<GuestOrder>
{
    public void Configure(EntityTypeBuilder<GuestOrder> builder)
    {
        builder.ToTable("GuestOrders");
        builder.Property(x => x.TableName).HasMaxLength(40);
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.GuestName).HasMaxLength(120);
        builder.Property(x => x.Note).HasMaxLength(240);
        builder.HasIndex(x => new { x.RestaurantId, x.ServiceDate, x.TokenNumber }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasOne(x => x.DiningTable).WithMany(x => x.Orders).HasForeignKey(x => x.DiningTableId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("OrderLines");
        builder.Property(x => x.ItemName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.VariantName).HasMaxLength(40);
        builder.Property(x => x.AddonSummary).HasMaxLength(200);
        builder.Property(x => x.Note).HasMaxLength(160);
        builder.Property(x => x.UnitPrice).HasPrecision(10, 2);
        builder.HasOne(x => x.GuestOrder).WithMany(x => x.Lines).HasForeignKey(x => x.GuestOrderId).OnDelete(DeleteBehavior.Restrict);
    }
}
