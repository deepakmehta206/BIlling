using Billing.Api.Http;
using Billing.Application.Abstractions;
using Billing.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/restaurants")]
public class RestaurantsController(IRestaurantService restaurants) : ControllerBase
{
    [Authorize(Policy = "SuperAdmin")]
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? q, CancellationToken cancellationToken) =>
        ApiResults.From(await restaurants.ListAsync(q, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        ApiResults.From(await restaurants.GetAsync(id, cancellationToken));

    [Authorize(Policy = "SuperAdmin")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateRestaurantRequest request, CancellationToken cancellationToken) =>
        ApiResults.From(await restaurants.CreateAsync(request, cancellationToken));

    [Authorize(Policy = "SuperAdmin")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateRestaurantRequest request, CancellationToken cancellationToken) =>
        ApiResults.From(await restaurants.UpdateAsync(id, request, cancellationToken));

    [Authorize(Policy = "SuperAdmin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        ApiResults.From(await restaurants.DeleteAsync(id, cancellationToken));

    [Authorize(Policy = "SuperAdmin")]
    [HttpPut("{id:guid}/modules")]
    public async Task<IActionResult> UpdateModules(Guid id, UpdateModulesRequest request, CancellationToken cancellationToken) =>
        ApiResults.From(await restaurants.UpdateModulesAsync(id, request, cancellationToken));
}
