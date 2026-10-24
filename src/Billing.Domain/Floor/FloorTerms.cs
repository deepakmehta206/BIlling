namespace Billing.Domain.Floor;

public static class FoodTypes
{
    public const string Veg = "Veg";
    public const string NonVeg = "NonVeg";
    public const string Egg = "Egg";

    public static bool Known(string value) => value is Veg or NonVeg or Egg;
}

public static class TableStatuses
{
    public const string Available = "Available";
    public const string Occupied = "Occupied";
    public const string Blocked = "Blocked";

    public static bool Known(string value) => value is Available or Occupied or Blocked;
}

public static class BookingStatuses
{
    public const string Booked = "Booked";
    public const string Seated = "Seated";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
}

public static class OrderStatuses
{
    public const string Preparing = "Preparing";
    public const string Ready = "Ready";
    public const string Served = "Served";
    public const string Cancelled = "Cancelled";

    public static bool Open(string value) => value is Preparing or Ready;

    public static bool Known(string value) => value is Preparing or Ready or Served or Cancelled;
}
