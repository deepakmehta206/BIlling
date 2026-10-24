namespace Billing.Domain.Billing;

public static class BillStatuses
{
    public const string Unpaid = "Unpaid";
    public const string Paid = "Paid";
    public const string Cancelled = "Cancelled";

    public static bool Known(string value) => value is Unpaid or Paid or Cancelled;
}

public static class PaymentModes
{
    public const string Cash = "Cash";
    public const string Upi = "Upi";

    public static bool Known(string value) => value is Cash or Upi;
}

public static class NoticeStatuses
{
    public const string Prepared = "Prepared";
    public const string Skipped = "Skipped";
}
