namespace Billing.Domain.Inventory;

public static class StockUnits
{
    public const string Gram = "g";
    public const string Kilogram = "kg";
    public const string Millilitre = "ml";
    public const string Litre = "L";
    public const string Piece = "pcs";

    public static bool Known(string value) => value is Gram or Kilogram or Millilitre or Litre or Piece;
}

public static class StockReasons
{
    public const string Purchase = "Purchase";
    public const string Sale = "Sale";
    public const string Adjust = "Adjust";
}
