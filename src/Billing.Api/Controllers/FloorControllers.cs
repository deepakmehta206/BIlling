using Billing.Api.Http;
using Billing.Application.Abstractions;
using Billing.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/menu")]
public class MenuController(IMenuService menu) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid? restaurantId, CancellationToken cancellationToken) =>
        ApiResults.From(await menu.GetAsync(restaurantId, cancellationToken));

    [HttpPost("categories")]
    public async Task<IActionResult> SaveCategory([FromQuery] Guid? restaurantId, SaveCategoryRequest request, CancellationToken cancellationToken) =>
        ApiResults.From(await menu.SaveCategoryAsync(restaurantId, request, cancellationToken));

    [HttpDelete("categories/{id:guid}")]
    public async Task<IActionResult> DeleteCategory([FromQuery] Guid? restaurantId, Guid id, CancellationToken cancellationToken) =>
        ApiResults.From(await menu.DeleteCategoryAsync(restaurantId, id, cancellationToken));

    [HttpPost("items")]
    public async Task<IActionResult> SaveItem([FromQuery] Guid? restaurantId, SaveItemRequest request, CancellationToken cancellationToken) =>
        ApiResults.From(await menu.SaveItemAsync(restaurantId, request, cancellationToken));

    [HttpDelete("items/{id:guid}")]
    public async Task<IActionResult> DeleteItem([FromQuery] Guid? restaurantId, Guid id, CancellationToken cancellationToken) =>
        ApiResults.From(await menu.DeleteItemAsync(restaurantId, id, cancellationToken));
}

[Authorize]
[ApiController]
[Route("api/tables")]
public class TablesController(IFloorService floor) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid? restaurantId, [FromQuery] DateTime? at, CancellationToken cancellationToken) =>
        ApiResults.From(await floor.GetTablesAsync(restaurantId, at, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Save([FromQuery] Guid? restaurantId, SaveTableRequest request, CancellationToken cancellationToken) =>
        ApiResults.From(await floor.SaveTableAsync(restaurantId, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([FromQuery] Guid? restaurantId, Guid id, CancellationToken cancellationToken) =>
        ApiResults.From(await floor.DeleteTableAsync(restaurantId, id, cancellationToken));
}

[Authorize]
[ApiController]
[Route("api/bookings")]
public class BookingsController(IFloorService floor) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid? restaurantId, [FromQuery] DateOnly? day, CancellationToken cancellationToken) =>
        ApiResults.From(await floor.GetBookingsAsync(restaurantId, day, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create([FromQuery] Guid? restaurantId, CreateBookingRequest request, CancellationToken cancellationToken) =>
        ApiResults.From(await floor.CreateBookingAsync(restaurantId, request, cancellationToken));

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel([FromQuery] Guid? restaurantId, Guid id, CancellationToken cancellationToken) =>
        ApiResults.From(await floor.CancelBookingAsync(restaurantId, id, cancellationToken));
}

[Authorize]
[ApiController]
[Route("api/orders")]
public class OrdersController(IFloorService floor) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid? restaurantId, CancellationToken cancellationToken) =>
        ApiResults.From(await floor.GetTicketsAsync(restaurantId, cancellationToken));

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> Status([FromQuery] Guid? restaurantId, Guid id, SetOrderStatusRequest request, CancellationToken cancellationToken) =>
        ApiResults.From(await floor.SetOrderStatusAsync(restaurantId, id, request, cancellationToken));
}

[AllowAnonymous]
[ApiController]
[Route("api/public")]
public class PublicFloorController(IFloorService floor) : ControllerBase
{
    [HttpGet("tables/{qrToken}")]
    public async Task<IActionResult> Table(string qrToken, CancellationToken cancellationToken) =>
        ApiResults.From(await floor.GetPublicTableAsync(qrToken, cancellationToken));

    [HttpPost("tables/{qrToken}/orders")]
    public async Task<IActionResult> Order(string qrToken, PlaceOrderRequest request, CancellationToken cancellationToken) =>
        ApiResults.From(await floor.PlacePublicOrderAsync(qrToken, request, cancellationToken));

    [HttpGet("orders/{orderId:guid}")]
    public async Task<IActionResult> OrderStatus(Guid orderId, CancellationToken cancellationToken) =>
        ApiResults.From(await floor.GetPublicOrderAsync(orderId, cancellationToken));

    [HttpPost("orders/{orderId:guid}/play")]
    public async Task<IActionResult> Play(Guid orderId, ClaimPlayRequest request, CancellationToken cancellationToken) =>
        ApiResults.From(await floor.ClaimPlayAsync(orderId, request, cancellationToken));

    [HttpGet("board/{code}")]
    public async Task<IActionResult> Board(string code, CancellationToken cancellationToken) =>
        ApiResults.From(await floor.GetPublicBoardAsync(code, cancellationToken));
}
