namespace Billing.Domain.Security;

public static class RoleNames
{
    public const string SuperAdmin = "SuperAdmin";
    public const string RestaurantAdmin = "RestaurantAdmin";
    public const string Manager = "Manager";
    public const string Cashier = "Cashier";
    public const string Waiter = "Waiter";
    public const string Kitchen = "Kitchen";
    public const string StoreKeeper = "StoreKeeper";
    public const string Hr = "Hr";

    public static readonly IReadOnlyList<string> HouseStaff =
    [
        Manager, Cashier, Waiter, Kitchen, StoreKeeper, Hr
    ];

    public static readonly IReadOnlyList<(string Name, string Description)> All =
    [
        (SuperAdmin, "Platform owner. Grants restaurants and modules."),
        (RestaurantAdmin, "Runs one restaurant and its staff."),
        (Manager, "Floor, bookings and day-end reports."),
        (Cashier, "Bills, payments and receipts."),
        (Waiter, "Tables and orders."),
        (Kitchen, "KOT and token status."),
        (StoreKeeper, "Inventory and purchases."),
        (Hr, "Employees, shifts and attendance.")
    ];
}
