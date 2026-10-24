using Billing.Api.Http;
using Billing.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/audit-logs")]
public class AuditLogsController(IAuditQueryService audits) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? restaurantId = null,
        [FromQuery] string? action = null,
        CancellationToken cancellationToken = default) =>
        ApiResults.From(await audits.ListAsync(page, pageSize, restaurantId, action, cancellationToken));
}
