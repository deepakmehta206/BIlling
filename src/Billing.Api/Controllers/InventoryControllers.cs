using Billing.Api.Http;
using Billing.Application.Abstractions;
using Billing.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/inventory")]
public class InventoryController(IInventoryService inventory) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid? restaurantId, CancellationToken cancellationToken) =>
        ApiResults.From(await inventory.GetAsync(restaurantId, cancellationToken));

    [HttpPost("items")]
    public async Task<IActionResult> SaveItem([FromQuery] Guid? restaurantId, SaveStockItemRequest request, CancellationToken cancellationToken) =>
        ApiResults.From(await inventory.SaveItemAsync(restaurantId, request, cancellationToken));

    [HttpDelete("items/{id:guid}")]
    public async Task<IActionResult> DeleteItem([FromQuery] Guid? restaurantId, Guid id, CancellationToken cancellationToken) =>
        ApiResults.From(await inventory.DeleteItemAsync(restaurantId, id, cancellationToken));

    [HttpPost("purchases")]
    public async Task<IActionResult> Purchase([FromQuery] Guid? restaurantId, PurchaseStockRequest request, CancellationToken cancellationToken) =>
        ApiResults.From(await inventory.PurchaseAsync(restaurantId, request, cancellationToken));

    [HttpPost("adjustments")]
    public async Task<IActionResult> Adjust([FromQuery] Guid? restaurantId, AdjustStockRequest request, CancellationToken cancellationToken) =>
        ApiResults.From(await inventory.AdjustAsync(restaurantId, request, cancellationToken));

    [HttpPut("recipes/{menuItemId:guid}")]
    public async Task<IActionResult> SaveRecipe([FromQuery] Guid? restaurantId, Guid menuItemId, SaveRecipeRequest request, CancellationToken cancellationToken) =>
        ApiResults.From(await inventory.SaveRecipeAsync(restaurantId, menuItemId, request, cancellationToken));
}
