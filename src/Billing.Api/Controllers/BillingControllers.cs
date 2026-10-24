using Billing.Api.Http;
using Billing.Application.Abstractions;
using Billing.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/bills")]
public class BillsController(IBillingService billing) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid? restaurantId, CancellationToken cancellationToken) =>
        ApiResults.From(await billing.GetDeskAsync(restaurantId, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create([FromQuery] Guid? restaurantId, CreateBillRequest request, CancellationToken cancellationToken) =>
        ApiResults.From(await billing.CreateAsync(restaurantId, request, cancellationToken));

    [HttpPost("{id:guid}/paid")]
    public async Task<IActionResult> Paid([FromQuery] Guid? restaurantId, Guid id, CancellationToken cancellationToken) =>
        ApiResults.From(await billing.MarkPaidAsync(restaurantId, id, cancellationToken));
}

[AllowAnonymous]
[ApiController]
[Route("api/public/bills")]
public class PublicBillsController(IBillingService billing) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        ApiResults.From(await billing.GetPublicAsync(id, cancellationToken));
}
