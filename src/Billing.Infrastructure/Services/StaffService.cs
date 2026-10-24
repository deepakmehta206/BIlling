using Billing.Application.Abstractions;
using Billing.Application.Common;
using Billing.Application.Dtos;
using Billing.Domain.Entities;
using Billing.Domain.Hrms;
using Billing.Domain.Security;
using Billing.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Billing.Infrastructure.Services;

public class StaffService(
    AppDbContext db,
    HouseAccess access,
    IRequestContext request,
    IHouseLive live,
    IPasswordHasher<AppUser> passwords) : IStaffService
{
    public async Task<ServiceResult<StaffDeskDto>> GetAsync(Guid? restaurantId, CancellationToken cancellationToken)
    {
        var house = await Open(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<StaffDeskDto>.Fail(house.StatusCode, house.Error ?? "Cannot open the staff book.");

        var people = await People(house.Value.Id, cancellationToken);
        var shiftNames = await ShiftNames(house.Value.Id, cancellationToken);
        var leaves = await Leaves(house.Value.Id, cancellationToken);
        var employees = people
            .Select(person => new EmployeeDto(
                person.Id,
                person.FullName,
                person.Email,
                person.Phone,
                person.Role,
                person.IsActive,
                shiftNames.GetValueOrDefault(person.Id)))
            .ToList();

        return ServiceResult<StaffDeskDto>.Ok(new StaffDeskDto(employees, leaves, RoleNames.HouseStaff.ToList()));
    }

    public async Task<ServiceResult<EmployeeDto>> HireAsync(Guid? restaurantId, SaveEmployeeRequest body, CancellationToken cancellationToken)
    {
        var house = await Open(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<EmployeeDto>.Fail(house.StatusCode, house.Error ?? "Cannot hire.");

        var parsed = await ReadPerson(body, requirePassword: true, cancellationToken);
        if (parsed.Error is not null || parsed.Role is null)
            return ServiceResult<EmployeeDto>.Fail(400, parsed.Error ?? "Check the employee.");

        if (await db.Users.AnyAsync(user => user.Email == parsed.Email, cancellationToken))
            return ServiceResult<EmployeeDto>.Fail(409, "That email is already used.");

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            RestaurantId = house.Value.Id,
            FullName = parsed.Name,
            Email = parsed.Email,
            Phone = parsed.Phone,
            IsActive = body.IsActive,
            IsSuperAdmin = false
        };
        user.PasswordHash = passwords.HashPassword(user, body.Password ?? string.Empty);
        db.Users.Add(user);
        db.UserRoles.Add(new UserRole { Id = Guid.NewGuid(), UserId = user.Id, RoleId = parsed.Role.Id });
        db.AuditLogs.Add(AuditEntries.Create(
            request,
            "staff.hired",
            "AppUser",
            user.Id.ToString(),
            $"Hired {user.FullName} as {parsed.Role.Name}.",
            house.Value.Id,
            house.Value.Name));
        await db.SaveChangesAsync(cancellationToken);
        await live.Changed(house.Value.Id, cancellationToken);
        return ServiceResult<EmployeeDto>.Ok(new EmployeeDto(user.Id, user.FullName, user.Email, user.Phone, parsed.Role.Name, user.IsActive, null));
    }

    public async Task<ServiceResult<EmployeeDto>> UpdateAsync(Guid? restaurantId, Guid userId, SaveEmployeeRequest body, CancellationToken cancellationToken)
    {
        var house = await Open(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<EmployeeDto>.Fail(house.StatusCode, house.Error ?? "Cannot update staff.");

        var user = await HouseUser(house.Value.Id, userId, cancellationToken);
        if (user is null)
            return ServiceResult<EmployeeDto>.Fail(404, "Employee not found.");

        if (await IsHouseAdmin(user.Id, cancellationToken))
            return ServiceResult<EmployeeDto>.Fail(403, "The house admin is changed from the restaurant page.");

        var parsed = await ReadPerson(body, requirePassword: false, cancellationToken);
        if (parsed.Error is not null || parsed.Role is null)
            return ServiceResult<EmployeeDto>.Fail(400, parsed.Error ?? "Check the employee.");

        if (await db.Users.AnyAsync(item => item.Id != user.Id && item.Email == parsed.Email, cancellationToken))
            return ServiceResult<EmployeeDto>.Fail(409, "That email is already used.");

        user.FullName = parsed.Name;
        user.Email = parsed.Email;
        user.Phone = parsed.Phone;
        user.IsActive = body.IsActive;
        if (!string.IsNullOrWhiteSpace(body.Password))
            user.PasswordHash = passwords.HashPassword(user, body.Password);

        var links = await db.UserRoles.Where(link => link.UserId == user.Id).ToListAsync(cancellationToken);
        if (links.All(link => link.RoleId != parsed.Role.Id) || links.Count != 1)
        {
            foreach (var link in links)
                db.UserRoles.Remove(link);
            db.UserRoles.Add(new UserRole { Id = Guid.NewGuid(), UserId = user.Id, RoleId = parsed.Role.Id });
        }

        db.AuditLogs.Add(AuditEntries.Create(
            request,
            "staff.updated",
            "AppUser",
            user.Id.ToString(),
            $"Updated {user.FullName} ({parsed.Role.Name}).",
            house.Value.Id,
            house.Value.Name));
        await db.SaveChangesAsync(cancellationToken);
        await live.Changed(house.Value.Id, cancellationToken);
        var shift = (await ShiftNames(house.Value.Id, cancellationToken)).GetValueOrDefault(user.Id);
        return ServiceResult<EmployeeDto>.Ok(new EmployeeDto(user.Id, user.FullName, user.Email, user.Phone, parsed.Role.Name, user.IsActive, shift));
    }

    public async Task<ServiceResult<MessageDto>> ArchiveAsync(Guid? restaurantId, Guid userId, CancellationToken cancellationToken)
    {
        var house = await Open(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<MessageDto>.Fail(house.StatusCode, house.Error ?? "Cannot archive staff.");

        if (request.UserId == userId)
            return ServiceResult<MessageDto>.Fail(403, "You cannot archive your own login.");

        var user = await HouseUser(house.Value.Id, userId, cancellationToken);
        if (user is null)
            return ServiceResult<MessageDto>.Fail(404, "Employee not found.");

        if (await IsHouseAdmin(user.Id, cancellationToken))
            return ServiceResult<MessageDto>.Fail(403, "The house admin stays with the restaurant.");

        foreach (var link in await db.UserRoles.Where(row => row.UserId == user.Id).ToListAsync(cancellationToken))
            db.UserRoles.Remove(link);
        foreach (var assignment in await db.ShiftAssignments.Where(row => row.UserId == user.Id).ToListAsync(cancellationToken))
            db.ShiftAssignments.Remove(assignment);
        foreach (var leave in await db.LeaveRequests.Where(row => row.UserId == user.Id && row.Status == LeaveStatuses.Pending).ToListAsync(cancellationToken))
            db.LeaveRequests.Remove(leave);

        db.Users.Remove(user);
        db.AuditLogs.Add(AuditEntries.Create(
            request,
            "staff.archived",
            "AppUser",
            user.Id.ToString(),
            $"{user.FullName} left the house.",
            house.Value.Id,
            house.Value.Name));
        await db.SaveChangesAsync(cancellationToken);
        await live.Changed(house.Value.Id, cancellationToken);
        return ServiceResult<MessageDto>.Ok(new MessageDto($"{user.FullName} archived."));
    }

    public async Task<ServiceResult<LeaveDto>> RequestLeaveAsync(Guid? restaurantId, SaveLeaveRequest body, CancellationToken cancellationToken)
    {
        var house = await Open(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<LeaveDto>.Fail(house.StatusCode, house.Error ?? "Cannot request leave.");

        if (!LeaveKinds.All.Contains(body.Kind))
            return ServiceResult<LeaveDto>.Fail(400, "Choose casual, sick, or unpaid leave.");
        if (body.To < body.From)
            return ServiceResult<LeaveDto>.Fail(400, "The leave end is before the start.");
        if (body.To.DayNumber - body.From.DayNumber > 30)
            return ServiceResult<LeaveDto>.Fail(400, "Leave can cover at most 31 days.");

        var user = await HouseUser(house.Value.Id, body.UserId, cancellationToken);
        if (user is null)
            return ServiceResult<LeaveDto>.Fail(404, "Employee not found.");

        var overlaps = await db.LeaveRequests.AnyAsync(
            row => row.RestaurantId == house.Value.Id
                && row.UserId == user.Id
                && (row.Status == LeaveStatuses.Pending || row.Status == LeaveStatuses.Approved)
                && row.FromDate <= body.To
                && row.ToDate >= body.From,
            cancellationToken);
        if (overlaps)
            return ServiceResult<LeaveDto>.Fail(409, "That person already has leave on those days.");

        var note = InputRules.Clean(body.Note);
        if (note is not null && note.Length > 200)
            note = note[..200];

        var leave = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            RestaurantId = house.Value.Id,
            UserId = user.Id,
            Kind = body.Kind,
            FromDate = body.From,
            ToDate = body.To,
            Status = LeaveStatuses.Pending,
            Note = note
        };
        db.LeaveRequests.Add(leave);
        db.AuditLogs.Add(AuditEntries.Create(
            request,
            "staff.leave_requested",
            "LeaveRequest",
            leave.Id.ToString(),
            $"{user.FullName} asked for {leave.Kind} leave.",
            house.Value.Id,
            house.Value.Name));
        await db.SaveChangesAsync(cancellationToken);
        await live.Changed(house.Value.Id, cancellationToken);
        return ServiceResult<LeaveDto>.Ok(MapLeave(leave, user.FullName));
    }

    public async Task<ServiceResult<LeaveDto>> DecideLeaveAsync(Guid? restaurantId, Guid leaveId, DecideLeaveRequest body, CancellationToken cancellationToken)
    {
        var house = await Open(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return ServiceResult<LeaveDto>.Fail(house.StatusCode, house.Error ?? "Cannot decide leave.");

        if (body.Status is not (LeaveStatuses.Approved or LeaveStatuses.Rejected))
            return ServiceResult<LeaveDto>.Fail(400, "Approve or reject the leave.");

        var leave = await db.LeaveRequests.FirstOrDefaultAsync(
            row => row.Id == leaveId && row.RestaurantId == house.Value.Id,
            cancellationToken);
        if (leave is null)
            return ServiceResult<LeaveDto>.Fail(404, "Leave not found.");
        if (leave.Status != LeaveStatuses.Pending)
            return ServiceResult<LeaveDto>.Fail(409, "This leave is already decided.");
        if (leave.UserId == request.UserId)
            return ServiceResult<LeaveDto>.Fail(403, "Someone else has to decide your leave.");

        var user = await HouseUser(house.Value.Id, leave.UserId, cancellationToken);
        leave.Status = body.Status;
        var name = user?.FullName ?? "Employee";
        db.AuditLogs.Add(AuditEntries.Create(
            request,
            "staff.leave_decided",
            "LeaveRequest",
            leave.Id.ToString(),
            $"Leave for {name} ({leave.Kind}) {leave.Status.ToLowerInvariant()}.",
            house.Value.Id,
            house.Value.Name));
        await db.SaveChangesAsync(cancellationToken);
        await live.Changed(house.Value.Id, cancellationToken);
        return ServiceResult<LeaveDto>.Ok(MapLeave(leave, name));
    }

    private async Task<ServiceResult<HouseScope>> Open(Guid? restaurantId, CancellationToken cancellationToken)
    {
        var house = await access.RequireHrms(restaurantId, cancellationToken);
        if (!house.Succeeded || house.Value is null)
            return house;

        return await access.CanManageStaff(cancellationToken)
            ? house
            : ServiceResult<HouseScope>.Fail(403, "Only the house admin or HR can open the staff book.");
    }

    private async Task<PersonFields> ReadPerson(SaveEmployeeRequest body, bool requirePassword, CancellationToken cancellationToken)
    {
        var name = InputRules.Required(body.FullName);
        var email = InputRules.Email(body.Email);
        var phone = InputRules.Required(body.Phone);
        if (name.Length < 2)
            return PersonFields.Bad("Enter the employee's name.");
        if (!InputRules.ValidEmail(email))
            return PersonFields.Bad("Enter a valid email.");
        if (!InputRules.ValidPhone(phone))
            return PersonFields.Bad("Phone needs 10 to 15 digits.");
        if (!RoleNames.HouseStaff.Contains(body.Role))
            return PersonFields.Bad("Choose a house role.");

        var password = body.Password ?? string.Empty;
        if (requirePassword || password.Length > 0)
        {
            if (!InputRules.ValidPassword(password))
                return PersonFields.Bad("Password needs 8 characters, with a letter and a number.");
        }

        var role = await db.Roles.FirstOrDefaultAsync(item => item.Name == body.Role, cancellationToken);
        return role is null
            ? PersonFields.Bad("That role is not seeded yet.")
            : new PersonFields(name, email, phone, role, null);
    }

    private Task<AppUser?> HouseUser(Guid restaurantId, Guid userId, CancellationToken cancellationToken) =>
        db.Users.FirstOrDefaultAsync(user => user.Id == userId && user.RestaurantId == restaurantId && !user.IsSuperAdmin, cancellationToken);

    private Task<bool> IsHouseAdmin(Guid userId, CancellationToken cancellationToken) =>
        db.UserRoles.AnyAsync(link => link.UserId == userId && link.Role.Name == RoleNames.RestaurantAdmin, cancellationToken);

    private async Task<List<PersonRow>> People(Guid restaurantId, CancellationToken cancellationToken)
    {
        var rows = await db.Users
            .Where(user => user.RestaurantId == restaurantId && !user.IsSuperAdmin)
            .OrderBy(user => user.FullName)
            .Select(user => new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.Phone,
                Role = user.UserRoles.Select(link => link.Role.Name).FirstOrDefault(),
                user.IsActive
            })
            .ToListAsync(cancellationToken);

        return rows.Select(row => new PersonRow(row.Id, row.FullName, row.Email, row.Phone, row.Role ?? string.Empty, row.IsActive)).ToList();
    }

    private async Task<Dictionary<Guid, string>> ShiftNames(Guid restaurantId, CancellationToken cancellationToken)
    {
        var rows = await (
            from assignment in db.ShiftAssignments
            join shift in db.Shifts on assignment.ShiftId equals shift.Id
            where assignment.RestaurantId == restaurantId
            select new { assignment.UserId, shift.Name }
        ).ToListAsync(cancellationToken);

        return rows.GroupBy(row => row.UserId).ToDictionary(group => group.Key, group => group.First().Name);
    }

    private async Task<List<LeaveDto>> Leaves(Guid restaurantId, CancellationToken cancellationToken)
    {
        var rows = await (
            from leave in db.LeaveRequests
            join user in db.Users on leave.UserId equals user.Id
            where leave.RestaurantId == restaurantId
            orderby leave.FromDate descending
            select new { leave.Id, leave.UserId, user.FullName, leave.Kind, leave.FromDate, leave.ToDate, leave.Status, leave.Note }
        ).Take(40).ToListAsync(cancellationToken);

        return rows
            .OrderBy(row => row.Status == LeaveStatuses.Pending ? 0 : 1)
            .ThenByDescending(row => row.FromDate)
            .Select(row => new LeaveDto(row.Id, row.UserId, row.FullName, row.Kind, row.FromDate, row.ToDate, row.Status, row.Note))
            .ToList();
    }

    private static LeaveDto MapLeave(LeaveRequest leave, string name) =>
        new(leave.Id, leave.UserId, name, leave.Kind, leave.FromDate, leave.ToDate, leave.Status, leave.Note);

    private sealed record PersonRow(Guid Id, string FullName, string Email, string? Phone, string Role, bool IsActive);

    private sealed record PersonFields(string Name, string Email, string Phone, Role? Role, string? Error)
    {
        public static PersonFields Bad(string error) => new(string.Empty, string.Empty, string.Empty, null, error);
    }
}
