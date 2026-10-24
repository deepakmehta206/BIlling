using Billing.Api.Http;
using Billing.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/reports")]
public class ReportsController(IReportService reports) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid? restaurantId, [FromQuery] string? date, CancellationToken cancellationToken) =>
        ApiResults.From(await reports.GetAsync(restaurantId, date, cancellationToken));
}
