namespace Billing.Domain.Hrms;

public static class LeaveKinds
{
    public const string Casual = "Casual";
    public const string Sick = "Sick";
    public const string Unpaid = "Unpaid";

    public static readonly string[] All = [Casual, Sick, Unpaid];
}

public static class LeaveStatuses
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
}

public static class AttendanceStates
{
    public const string Away = "Away";
    public const string In = "In";
    public const string Late = "Late";
    public const string Out = "Out";
    public const string Leave = "Leave";
}
