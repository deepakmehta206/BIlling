namespace Billing.Application.Dtos;

public sealed class CreateRestaurantRequest
{
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? Code { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Gstin { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerPhone { get; set; } = string.Empty;
    public string? UpiId { get; set; }
    public string AdminFullName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public string AdminPhone { get; set; } = string.Empty;
}

public sealed class UpdateRestaurantRequest
{
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? Code { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Gstin { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerPhone { get; set; } = string.Empty;
    public string? UpiId { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UpdateModulesRequest
{
    public List<ModuleToggle> Modules { get; set; } = [];
}

public sealed class ModuleToggle
{
    public string Code { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}

public sealed record ModuleDto(string Code, string Name, string Description, bool IsEnabled, int SortOrder);

public sealed record AdminUserDto(Guid Id, string FullName, string Email, string? Phone, bool IsActive);

public sealed record RestaurantListItemDto(
    Guid Id,
    string Name,
    string Code,
    string? City,
    string? Phone,
    string OwnerName,
    string OwnerPhone,
    bool IsActive,
    int EnabledModules,
    int TotalModules,
    string? AdminEmail,
    string? CreatedIp,
    DateTime CreatedAt);

public sealed record RestaurantDetailDto(
    Guid Id,
    string Name,
    string Code,
    string? LegalName,
    string? Address,
    string? City,
    string? State,
    string? Phone,
    string? Email,
    string? Gstin,
    string OwnerName,
    string OwnerPhone,
    string? UpiId,
    bool IsActive,
    DateTime CreatedAt,
    string? CreatedIp,
    DateTime? UpdatedAt,
    string? UpdatedIp,
    AdminUserDto? Admin,
    IReadOnlyList<ModuleDto> Modules);

public sealed record MessageDto(string Message);
