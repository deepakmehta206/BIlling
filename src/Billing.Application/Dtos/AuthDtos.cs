namespace Billing.Application.Dtos;

public sealed class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed record LoginResponse(string AccessToken, DateTime ExpiresAt, CurrentUserDto User);

public sealed record CurrentUserDto(
    Guid Id,
    string FullName,
    string Email,
    bool IsSuperAdmin,
    Guid? RestaurantId,
    string? RestaurantName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> EnabledModules);
