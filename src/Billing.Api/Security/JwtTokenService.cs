using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Billing.Application.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace Billing.Api.Security;

public sealed class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    public (string Token, DateTime ExpiresAt) Create(
        Guid userId,
        string email,
        string fullName,
        bool isSuperAdmin,
        Guid? restaurantId,
        IReadOnlyList<string> roles)
    {
        var section = configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(section["Key"]!));
        var minutes = int.TryParse(section["ExpiresMinutes"], out var parsed) ? parsed : 720;
        var expires = DateTime.UtcNow.AddMinutes(minutes);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, fullName),
            new("super_admin", isSuperAdmin ? "true" : "false")
        };

        if (restaurantId is Guid restaurant)
            claims.Add(new Claim("restaurant_id", restaurant.ToString()));

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            section["Issuer"],
            section["Audience"],
            claims,
            expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
