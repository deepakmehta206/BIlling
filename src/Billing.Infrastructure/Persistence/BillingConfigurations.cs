using Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.Infrastructure.Persistence;

public class BillConfiguration : IEntityTypeConfiguration<Bill>
{
    public void Configure(EntityTypeBuilder<Bill> builder)
    {
        builder.ToTable("Bills");
        builder.Property(x => x.TableName).HasMaxLength(40);
        builder.Property(x => x.GuestName).HasMaxLength(120);
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.PaymentMode).HasMaxLength(12).IsRequired();
        builder.Property(x => x.UpiUri).HasMaxLength(500);
        builder.Property(x => x.Subtotal).HasPrecision(10, 2);
        builder.Property(x => x.GstPercent).HasPrecision(5, 2);
        builder.Property(x => x.GstAmount).HasPrecision(10, 2);
        builder.Property(x => x.Discount).HasPrecision(10, 2);
        builder.Property(x => x.Total).HasPrecision(10, 2);
        builder.HasIndex(x => new { x.RestaurantId, x.ServiceDate, x.BillNumber }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => x.GuestOrderId).IsUnique().HasFilter("[IsDeleted] = 0 AND [GuestOrderId] IS NOT NULL");
        builder.HasOne(x => x.Restaurant).WithMany().HasForeignKey(x => x.RestaurantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.GuestOrder).WithMany().HasForeignKey(x => x.GuestOrderId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class BillLineConfiguration : IEntityTypeConfiguration<BillLine>
{
    public void Configure(EntityTypeBuilder<BillLine> builder)
    {
        builder.ToTable("BillLines");
        builder.Property(x => x.ItemName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.VariantName).HasMaxLength(40);
        builder.Property(x => x.UnitPrice).HasPrecision(10, 2);
        builder.HasOne(x => x.Bill).WithMany(x => x.Lines).HasForeignKey(x => x.BillId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class OwnerNoticeConfiguration : IEntityTypeConfiguration<OwnerNotice>
{
    public void Configure(EntityTypeBuilder<OwnerNotice> builder)
    {
        builder.ToTable("OwnerNotices");
        builder.Property(x => x.Phone).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Body).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.WaUrl).HasMaxLength(4000);
        builder.HasOne(x => x.Bill).WithMany(x => x.Notices).HasForeignKey(x => x.BillId).OnDelete(DeleteBehavior.Restrict);
    }
}
