namespace Billing.Application.Common;

public static class InputRules
{
    public static string? Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    public static string Required(string? value) => Clean(value) ?? string.Empty;

    public static string Email(string? value) => Required(value).ToLowerInvariant();

    public static bool ValidEmail(string email) =>
        email.Contains('@') && email.IndexOf('@') > 0 && email.IndexOf('@') < email.Length - 1 && !email.Contains(' ');

    public static bool ValidPassword(string password) =>
        password.Length >= 8 && password.Any(char.IsLetter) && password.Any(char.IsDigit);

    public static bool ValidPhone(string? value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits.Length is >= 10 and <= 15;
    }

    public static bool ValidGstin(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        var gstin = value.Trim();
        return gstin.Length == 15 && gstin.All(char.IsLetterOrDigit);
    }

    public static bool ValidUpi(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        var upi = value.Trim();
        var at = upi.IndexOf('@');
        return at > 0 && at < upi.Length - 1 && !upi.Contains(' ');
    }

    public static string? TrimTo(string? value, int max)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return value.Length <= max ? value : value[..max];
    }
}
