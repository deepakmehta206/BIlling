using Billing.Application.Abstractions;
using Billing.Application.Common;
using Billing.Application.Dtos;
using Billing.Domain.Entities;
using Billing.Domain.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Billing.Infrastructure.Services;

public class AuthService(
    Persistence.AppDbContext db,
    IRequestContext request,
    IPasswordHasher<AppUser> passwords,
    IJwtTokenService tokens) : IAuthService
{
    public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest requestBody, CancellationToken cancellationToken)
    {
        var email = InputRules.Email(requestBody.Email);
        var password = requestBody.Password ?? string.Empty;
        if (!InputRules.ValidEmail(email) || password.Length == 0)
            return ServiceResult<LoginResponse>.Fail(400, "Enter the email and password.");

        var user = await db.Users
            .Include(item => item.Restaurant)
            .Include(item => item.UserRoles)
            .ThenInclude(item => item.Role)
            .FirstOrDefaultAsync(item => item.Email == email, cancellationToken);

        if (user is null || passwords.VerifyHashedPassword(user, user.PasswordHash, password) == PasswordVerificationResult.Failed)
        {
            AddLoginAudit("auth.login_failed", user, email, "Sign-in failed.");
            await db.SaveChangesAsync(cancellationToken);
            return ServiceResult<LoginResponse>.Fail(401, "Email or password is incorrect.");
        }

        if (!user.IsActive)
        {
            AddLoginAudit("auth.login_denied", user, email, "Sign-in blocked because the account is inactive.");
            await db.SaveChangesAsync(cancellationToken);
            return ServiceResult<LoginResponse>.Fail(403, "This account is inactive.");
        }

        if (!user.IsSuperAdmin)
        {
            if (user.Restaurant is null || !user.Restaurant.IsActive)
            {
                var reason = user.Restaurant is null
                    ? "This restaurant is no longer on the platform."
                    : "This restaurant is paused. Ask the platform admin.";
                AddLoginAudit("auth.login_denied", user, email, reason);
                await db.SaveChangesAsync(cancellationToken);
                return ServiceResult<LoginResponse>.Fail(403, reason);
            }
        }

        user.LastLoginAt = DateTime.UtcNow;
        user.LastLoginIp = InputRules.TrimTo(request.IpAddress, 64);

        var roles = user.UserRoles.Select(item => item.Role.Name).Distinct().ToList();
        var enabled = await EnabledModulesAsync(user, cancellationToken);
        var (token, expires) = tokens.Create(user.Id, user.Email, user.FullName, user.IsSuperAdmin, user.RestaurantId, roles);

        AddLoginAudit("auth.login", user, user.Email, $"{user.FullName} signed in.");
        await db.SaveChangesAsync(cancellationToken);

        return ServiceResult<LoginResponse>.Ok(new LoginResponse(token, expires, ToUser(user, roles, enabled)));
    }

    public async Task<ServiceResult<CurrentUserDto>> MeAsync(CancellationToken cancellationToken)
    {
        if (request.UserId is not Guid userId)
            return ServiceResult<CurrentUserDto>.Fail(401, "Sign in again.");

        var user = await db.Users
            .Include(item => item.Restaurant)
            .Include(item => item.UserRoles)
            .ThenInclude(item => item.Role)
            .FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);

        if (user is null || !user.IsActive)
            return ServiceResult<CurrentUserDto>.Fail(401, "Sign in again.");

        var roles = user.UserRoles.Select(item => item.Role.Name).Distinct().ToList();
        var enabled = await EnabledModulesAsync(user, cancellationToken);
        return ServiceResult<CurrentUserDto>.Ok(ToUser(user, roles, enabled));
    }

    private async Task<IReadOnlyList<string>> EnabledModulesAsync(AppUser user, CancellationToken cancellationToken)
    {
        if (user.IsSuperAdmin)
            return ModuleCodes.All.Select(item => item.Code).ToList();

        if (user.RestaurantId is not Guid restaurantId)
            return [];

        return await db.RestaurantModules
            .Where(item => item.RestaurantId == restaurantId && item.IsEnabled)
            .OrderBy(item => item.Module.SortOrder)
            .Select(item => item.Module.Code)
            .ToListAsync(cancellationToken);
    }

    private void AddLoginAudit(string action, AppUser? user, string email, string description)
    {
        var entry = Persistence.AuditEntries.Create(
            request,
            action,
            "User",
            user?.Id.ToString(),
            description,
            user?.RestaurantId,
            user?.Restaurant?.Name,
            email);
        entry.ActorId = user?.Id;
        db.AuditLogs.Add(entry);
    }

    private static CurrentUserDto ToUser(AppUser user, IReadOnlyList<string> roles, IReadOnlyList<string> modules) =>
        new(user.Id, user.FullName, user.Email, user.IsSuperAdmin, user.RestaurantId, user.Restaurant?.Name, roles, modules);
}
