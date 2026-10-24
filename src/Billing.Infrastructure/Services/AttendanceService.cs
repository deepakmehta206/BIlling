using Billing.Application.Abstractions;
using Billing.Application.Common;
using Billing.Application.Dtos;
using Billing.Domain.Entities;
using Billing.Domain.Hrms;
using Billing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Billing.Infrastructure.Services;

public class AttendanceService(AppDbContext db, HouseAccess access, IRequestContext request, IHouseLive live) : IAttendanceService
{
    public async Task<ServiceResult<AttendanceDeskDto>> GetAsync(Guid? restaurantId, CancellationToken cancellationToken)
    {
        var house = await access.RequireAttendance(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<AttendanceDeskDto>.Fail(house.StatusCode, house.Error ?? "Cannot open attendance.");

        var canManage = await access.CanManageStaff(cancellationToken);
        var today = IndiaClock.Today();
        var shifts = await db.Shifts
            .Where(shift => shift.RestaurantId == house.Value.Id)
            .OrderBy(shift => shift.StartsAt)
            .Select(shift => new ShiftDto(shift.Id, shift.Name, shift.StartsAt.ToString("HH:mm"), shift.EndsAt.ToString("HH:mm"), shift.GraceMinutes))
            .ToListAsync(cancellationToken);

        var people = await db.Users
            .Where(user => user.RestaurantId == house.Value.Id && !user.IsSuperAdmin && user.IsActive)
            .OrderBy(user => user.FullName)
            .Select(user => new
            {
                user.Id,
                user.FullName,
                Role = user.UserRoles.Select(link => link.Role.Name).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        if (!canManage && request.UserId is Guid me)
            people = people.Where(person => person.Id == me).ToList();

        var assignments = await (
            from assignment in db.ShiftAssignments
            join shift in db.Shifts on assignment.ShiftId equals shift.Id
            where assignment.RestaurantId == house.Value.Id
            select new { assignment.UserId, shift.Name, shift.StartsAt, shift.GraceMinutes }
        ).ToListAsync(cancellationToken);
        var byUser = assignments.GroupBy(row => row.UserId).ToDictionary(group => group.Key, group => group.First());

        var marks = await db.AttendanceMarks
            .Where(mark => mark.RestaurantId == house.Value.Id && mark.ServiceDate == today)
            .ToListAsync(cancellationToken);
        var markByUser = marks.ToDictionary(mark => mark.UserId);

        var onLeave = await db.LeaveRequests
            .Where(leave => leave.RestaurantId == house.Value.Id
                && leave.Status == LeaveStatuses.Approved
                && leave.FromDate <= today
                && leave.ToDate >= today)
            .Select(leave => leave.UserId)
            .ToListAsync(cancellationToken);

        var rows = people.Select(person =>
        {
            byUser.TryGetValue(person.Id, out var shift);
            markByUser.TryGetValue(person.Id, out var mark);
            var state = mark?.CheckOutAt is not null
                ? AttendanceStates.Out
                : mark is not null && mark.Late
                    ? AttendanceStates.Late
                    : mark is not null
                        ? AttendanceStates.In
                        : onLeave.Contains(person.Id)
                            ? AttendanceStates.Leave
                            : AttendanceStates.Away;
            return new AttendanceRowDto(
                person.Id,
                person.FullName,
                person.Role ?? string.Empty,
                shift?.Name,
                mark?.CheckInAt,
                mark?.CheckOutAt,
                mark?.Late ?? false,
                state);
        }).ToList();

        return ServiceResult<AttendanceDeskDto>.Ok(new AttendanceDeskDto(shifts, rows, canManage));
    }

    public async Task<ServiceResult<ShiftDto>> SaveShiftAsync(Guid? restaurantId, SaveShiftRequest body, CancellationToken cancellationToken)
    {
        var house = await OpenManaged(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<ShiftDto>.Fail(house.StatusCode, house.Error ?? "Cannot save a shift.");

        var name = InputRules.Required(body.Name);
        if (name.Length is < 2 or > 40)
            return ServiceResult<ShiftDto>.Fail(400, "Shift name needs 2 to 40 characters.");
        if (!TimeOnly.TryParse(body.StartsAt, out var starts) || !TimeOnly.TryParse(body.EndsAt, out var ends))
            return ServiceResult<ShiftDto>.Fail(400, "Enter a start and end time.");
        if (ends <= starts)
            return ServiceResult<ShiftDto>.Fail(400, "The shift ends after it starts, on the same day.");
        if (body.GraceMinutes is < 0 or > 90)
            return ServiceResult<ShiftDto>.Fail(400, "Grace is between 0 and 90 minutes.");
        if (starts.Add(TimeSpan.FromMinutes(body.GraceMinutes)) < starts)
            return ServiceResult<ShiftDto>.Fail(400, "Grace stays inside the same day.");

        if (await db.Shifts.AnyAsync(shift => shift.RestaurantId == house.Value.Id && shift.Name == name, cancellationToken))
            return ServiceResult<ShiftDto>.Fail(409, "That shift name is already used.");

        var shift = new Shift
        {
            Id = Guid.NewGuid(),
            RestaurantId = house.Value.Id,
            Name = name,
            StartsAt = starts,
            EndsAt = ends,
            GraceMinutes = body.GraceMinutes
        };
        db.Shifts.Add(shift);
        db.AuditLogs.Add(AuditEntries.Create(
            request,
            "attendance.shift_saved",
            "Shift",
            shift.Id.ToString(),
            $"{shift.Name} runs {shift.StartsAt:HH:mm}–{shift.EndsAt:HH:mm}.",
            house.Value.Id,
            house.Value.Name));
        await db.SaveChangesAsync(cancellationToken);
        await live.Changed(house.Value.Id, cancellationToken);
        return ServiceResult<ShiftDto>.Ok(new ShiftDto(shift.Id, shift.Name, shift.StartsAt.ToString("HH:mm"), shift.EndsAt.ToString("HH:mm"), shift.GraceMinutes));
    }

    public async Task<ServiceResult<MessageDto>> ArchiveShiftAsync(Guid? restaurantId, Guid shiftId, CancellationToken cancellationToken)
    {
        var house = await OpenManaged(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<MessageDto>.Fail(house.StatusCode, house.Error ?? "Cannot archive a shift.");

        var shift = await db.Shifts.FirstOrDefaultAsync(row => row.Id == shiftId && row.RestaurantId == house.Value.Id, cancellationToken);
        if (shift is null)
            return ServiceResult<MessageDto>.Fail(404, "Shift not found.");

        foreach (var assignment in await db.ShiftAssignments.Where(row => row.ShiftId == shift.Id).ToListAsync(cancellationToken))
            db.ShiftAssignments.Remove(assignment);

        db.Shifts.Remove(shift);
        db.AuditLogs.Add(AuditEntries.Create(
            request,
            "attendance.shift_archived",
            "Shift",
            shift.Id.ToString(),
            $"{shift.Name} was archived.",
            house.Value.Id,
            house.Value.Name));
        await db.SaveChangesAsync(cancellationToken);
        await live.Changed(house.Value.Id, cancellationToken);
        return ServiceResult<MessageDto>.Ok(new MessageDto($"{shift.Name} archived."));
    }

    public async Task<ServiceResult<MessageDto>> AssignAsync(Guid? restaurantId, AssignShiftRequest body, CancellationToken cancellationToken)
    {
        var house = await OpenManaged(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<MessageDto>.Fail(house.StatusCode, house.Error ?? "Cannot assign a shift.");

        var user = await db.Users.FirstOrDefaultAsync(
            row => row.Id == body.UserId && row.RestaurantId == house.Value.Id && !row.IsSuperAdmin && row.IsActive,
            cancellationToken);
        var shift = await db.Shifts.FirstOrDefaultAsync(row => row.Id == body.ShiftId && row.RestaurantId == house.Value.Id, cancellationToken);
        if (user is null || shift is null)
            return ServiceResult<MessageDto>.Fail(404, "Choose a person and a shift from this house.");

        var current = await db.ShiftAssignments.FirstOrDefaultAsync(
            row => row.RestaurantId == house.Value.Id && row.UserId == user.Id,
            cancellationToken);
        if (current is null)
        {
            db.ShiftAssignments.Add(new ShiftAssignment
            {
                Id = Guid.NewGuid(),
                RestaurantId = house.Value.Id,
                UserId = user.Id,
                ShiftId = shift.Id
            });
        }
        else
        {
            current.ShiftId = shift.Id;
        }

        db.AuditLogs.Add(AuditEntries.Create(
            request,
            "attendance.assigned",
            "ShiftAssignment",
            user.Id.ToString(),
            $"{user.FullName} works {shift.Name}.",
            house.Value.Id,
            house.Value.Name));
        await db.SaveChangesAsync(cancellationToken);
        await live.Changed(house.Value.Id, cancellationToken);
        return ServiceResult<MessageDto>.Ok(new MessageDto($"{user.FullName} works {shift.Name}."));
    }

    public async Task<ServiceResult<MessageDto>> CheckInAsync(Guid? restaurantId, MarkAttendanceRequest body, CancellationToken cancellationToken)
    {
        var house = await access.RequireAttendance(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<MessageDto>.Fail(house.StatusCode, house.Error ?? "Cannot check in.");

        var user = await PunchUser(house.Value.Id, body.UserId, cancellationToken);
        if (user.Error is not null || user.Person is null)
            return ServiceResult<MessageDto>.Fail(user.Status, user.Error ?? "Cannot check in.");

        var today = IndiaClock.Today();
        if (await db.AttendanceMarks.AnyAsync(
            mark => mark.RestaurantId == house.Value.Id && mark.UserId == user.Person.Id && mark.ServiceDate == today,
            cancellationToken))
            return ServiceResult<MessageDto>.Fail(409, "Already marked in today.");

        var assignment = await (
            from row in db.ShiftAssignments
            join shift in db.Shifts on row.ShiftId equals shift.Id
            where row.RestaurantId == house.Value.Id && row.UserId == user.Person.Id
            select new { shift.Name, shift.StartsAt, shift.GraceMinutes }
        ).FirstOrDefaultAsync(cancellationToken);

        var at = DateTime.UtcNow;
        var late = assignment is not null && IndiaClock.ArrivedLate(assignment.StartsAt, assignment.GraceMinutes, at);
        db.AttendanceMarks.Add(new AttendanceMark
        {
            Id = Guid.NewGuid(),
            RestaurantId = house.Value.Id,
            UserId = user.Person.Id,
            ServiceDate = today,
            CheckInAt = at,
            ShiftName = assignment?.Name,
            Late = late
        });
        db.AuditLogs.Add(AuditEntries.Create(
            request,
            "attendance.checked_in",
            "AttendanceMark",
            user.Person.Id.ToString(),
            late ? $"{user.Person.FullName} checked in late." : $"{user.Person.FullName} checked in.",
            house.Value.Id,
            house.Value.Name));
        await db.SaveChangesAsync(cancellationToken);
        await live.Changed(house.Value.Id, cancellationToken);
        return ServiceResult<MessageDto>.Ok(new MessageDto(late ? $"{user.Person.FullName} is late." : $"{user.Person.FullName} is in."));
    }

    public async Task<ServiceResult<MessageDto>> CheckOutAsync(Guid? restaurantId, MarkAttendanceRequest body, CancellationToken cancellationToken)
    {
        var house = await access.RequireAttendance(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<MessageDto>.Fail(house.StatusCode, house.Error ?? "Cannot check out.");

        var user = await PunchUser(house.Value.Id, body.UserId, cancellationToken);
        if (user.Error is not null || user.Person is null)
            return ServiceResult<MessageDto>.Fail(user.Status, user.Error ?? "Cannot check out.");

        var today = IndiaClock.Today();
        var mark = await db.AttendanceMarks.FirstOrDefaultAsync(
            row => row.RestaurantId == house.Value.Id && row.UserId == user.Person.Id && row.ServiceDate == today,
            cancellationToken);
        if (mark is null)
            return ServiceResult<MessageDto>.Fail(404, "No check-in for today.");
        if (mark.CheckOutAt is not null)
            return ServiceResult<MessageDto>.Fail(409, "Already marked out.");

        mark.CheckOutAt = DateTime.UtcNow;
        db.AuditLogs.Add(AuditEntries.Create(
            request,
            "attendance.checked_out",
            "AttendanceMark",
            user.Person.Id.ToString(),
            $"{user.Person.FullName} checked out.",
            house.Value.Id,
            house.Value.Name));
        await db.SaveChangesAsync(cancellationToken);
        await live.Changed(house.Value.Id, cancellationToken);
        return ServiceResult<MessageDto>.Ok(new MessageDto($"{user.Person.FullName} is out."));
    }

    private async Task<ServiceResult<HouseScope>> OpenManaged(Guid? restaurantId, CancellationToken cancellationToken)
    {
        var house = await access.RequireAttendance(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return house;

        return await access.CanManageStaff(cancellationToken)
            ? house
            : ServiceResult<HouseScope>.Fail(403, "Only the house admin or HR can change shifts.");
    }

    private async Task<(AppUser? Person, int Status, string? Error)> PunchUser(Guid restaurantId, Guid userId, CancellationToken cancellationToken)
    {
        var canManage = await access.CanManageStaff(cancellationToken);
        if (!canManage && request.UserId != userId)
            return (null, 403, "You can only mark your own attendance.");

        var user = await db.Users.FirstOrDefaultAsync(
            row => row.Id == userId && row.RestaurantId == restaurantId && !row.IsSuperAdmin && row.IsActive,
            cancellationToken);
        return user is null
            ? (null, 404, "Employee not found.")
            : (user, 200, null);
    }
}
