namespace Billing.Domain.Security;

public static class ModuleCodes
{
    public const string Billing = "BILLING";
    public const string Menu = "MENU";
    public const string Tables = "TABLES";
    public const string QrOrder = "QR_ORDER";
    public const string TokenDisplay = "TOKEN_DISPLAY";
    public const string Booking = "BOOKING";
    public const string Inventory = "INVENTORY";
    public const string Hrms = "HRMS";
    public const string Attendance = "ATTENDANCE";
    public const string Reports = "REPORTS";
    public const string WhatsApp = "WHATSAPP";
    public const string Upi = "UPI";

    public static readonly IReadOnlyList<ModuleDefinition> All =
    [
        new(Billing, "Billing", "POS, KOT and the final bill.", 1, true),
        new(Menu, "Menu", "Categories, dishes, variants and add-ons.", 2, true),
        new(Tables, "Tables", "Visual floor. Empty, occupied and blocked tables.", 3, true),
        new(QrOrder, "QR order", "Guests scan the table and place an order.", 4, true),
        new(TokenDisplay, "Token display", "Kitchen screen shows when an order is ready.", 5, true),
        new(Booking, "Table booking", "Online reservations on the same floor plan.", 6, true),
        new(Reports, "Reports", "Sales, tax and day-end.", 7, true),
        new(Inventory, "Inventory", "Stock, recipes and purchases.", 8, false),
        new(Hrms, "HRMS", "Employees, roles and leave.", 9, false),
        new(Attendance, "Attendance", "Check-in, shifts and late marks.", 10, false),
        new(WhatsApp, "WhatsApp", "Owner gets the bill on WhatsApp.", 11, false),
        new(Upi, "UPI QR", "Guest pays from a dynamic UPI QR.", 12, false)
    ];
}

public sealed record ModuleDefinition(string Code, string Name, string Description, int SortOrder, bool EnabledByDefault);
