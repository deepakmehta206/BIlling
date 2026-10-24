using Billing.Application.Abstractions;
using Billing.Application.Common;
using Billing.Domain.Entities;

namespace Billing.Infrastructure.Persistence;

internal static class AuditEntries
{
    public static AuditLog Create(
        IRequestContext context,
        string action,
        string entityName,
        string? entityId,
        string description,
        Guid? restaurantId = null,
        string? restaurantName = null,
        string? actorEmail = null) =>
        new()
        {
            ActorId = context.UserId,
            ActorEmail = actorEmail ?? context.Email,
            RestaurantId = restaurantId,
            RestaurantName = restaurantName,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Description = description.Length <= 1000 ? description : description[..1000],
            UserAgent = InputRules.TrimTo(context.UserAgent, 512)
        };
}

internal static class RestaurantCodes
{
    public static string ToCode(string name)
    {
        var builder = new System.Text.StringBuilder();
        var dash = false;
        foreach (var character in name.Trim().ToUpperInvariant())
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                builder.Append(character);
                dash = false;
            }
            else if (!dash && builder.Length > 0)
            {
                builder.Append('-');
                dash = true;
            }
        }

        var code = builder.ToString().Trim('-');
        if (code.Length == 0)
            code = "HOUSE";

        return code.Length <= 24 ? code : code[..24].Trim('-');
    }
}
