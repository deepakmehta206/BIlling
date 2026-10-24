namespace Billing.Application.Dtos;

public sealed record EmployeeDto(
    Guid Id,
    string FullName,
    string Email,
    string? Phone,
    string Role,
    bool IsActive,
    string? ShiftName);

public sealed record LeaveDto(
    Guid Id,
    Guid UserId,
    string EmployeeName,
    string Kind,
    DateOnly From,
    DateOnly To,
    string Status,
    string? Note);

public sealed record StaffDeskDto(
    IReadOnlyList<EmployeeDto> Employees,
    IReadOnlyList<LeaveDto> Leaves,
    IReadOnlyList<string> Roles);

public sealed class SaveEmployeeRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Password { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class SaveLeaveRequest
{
    public Guid UserId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public string? Note { get; set; }
}

public sealed class DecideLeaveRequest
{
    public string Status { get; set; } = string.Empty;
}

public sealed record ShiftDto(Guid Id, string Name, string StartsAt, string EndsAt, int GraceMinutes);

public sealed record AttendanceRowDto(
    Guid UserId,
    string FullName,
    string Role,
    string? ShiftName,
    DateTime? CheckInAt,
    DateTime? CheckOutAt,
    bool Late,
    string State);

public sealed record AttendanceDeskDto(
    IReadOnlyList<ShiftDto> Shifts,
    IReadOnlyList<AttendanceRowDto> Rows,
    bool CanManage);

public sealed class SaveShiftRequest
{
    public string Name { get; set; } = string.Empty;
    public string StartsAt { get; set; } = string.Empty;
    public string EndsAt { get; set; } = string.Empty;
    public int GraceMinutes { get; set; }
}

public sealed class AssignShiftRequest
{
    public Guid UserId { get; set; }
    public Guid ShiftId { get; set; }
}

public sealed class MarkAttendanceRequest
{
    public Guid UserId { get; set; }
}
