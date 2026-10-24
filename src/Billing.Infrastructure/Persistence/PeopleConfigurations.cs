using Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Billing.Infrastructure.Persistence;

public class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        builder.ToTable("Shifts");
        builder.Property(x => x.Name).HasMaxLength(40).IsRequired();
        builder.HasIndex(x => new { x.RestaurantId, x.Name }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasOne(x => x.Restaurant).WithMany().HasForeignKey(x => x.RestaurantId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ShiftAssignmentConfiguration : IEntityTypeConfiguration<ShiftAssignment>
{
    public void Configure(EntityTypeBuilder<ShiftAssignment> builder)
    {
        builder.ToTable("ShiftAssignments");
        builder.HasIndex(x => new { x.RestaurantId, x.UserId }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Shift).WithMany().HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable("LeaveRequests");
        builder.Property(x => x.Kind).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(200);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AttendanceMarkConfiguration : IEntityTypeConfiguration<AttendanceMark>
{
    public void Configure(EntityTypeBuilder<AttendanceMark> builder)
    {
        builder.ToTable("AttendanceMarks");
        builder.Property(x => x.ShiftName).HasMaxLength(40);
        builder.HasIndex(x => new { x.RestaurantId, x.UserId, x.ServiceDate }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
