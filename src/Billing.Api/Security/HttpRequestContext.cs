using System.Security.Claims;
using Billing.Application.Abstractions;

namespace Billing.Api.Security;

public sealed class HttpRequestContext(IHttpContextAccessor accessor) : IRequestContext
{
    public bool IsAuthenticated => accessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public Guid? UserId =>
        Guid.TryParse(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? Email => accessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email);

    public bool IsSuperAdmin =>
        string.Equals(accessor.HttpContext?.User.FindFirstValue("super_admin"), "true", StringComparison.OrdinalIgnoreCase);

    public Guid? RestaurantId =>
        Guid.TryParse(accessor.HttpContext?.User.FindFirstValue("restaurant_id"), out var id) ? id : null;

    public string? IpAddress
    {
        get
        {
            var http = accessor.HttpContext;
            if (http is null)
                return null;

            var forwarded = http.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwarded))
            {
                var first = forwarded.Split(',')[0].Trim();
                if (first.Length > 0)
                    return first.Length <= 64 ? first : first[..64];
            }

            return http.Connection.RemoteIpAddress?.ToString();
        }
    }

    public string? UserAgent
    {
        get
        {
            var agent = accessor.HttpContext?.Request.Headers.UserAgent.ToString();
            if (string.IsNullOrWhiteSpace(agent))
                return null;

            return agent.Length <= 512 ? agent : agent[..512];
        }
    }
}
