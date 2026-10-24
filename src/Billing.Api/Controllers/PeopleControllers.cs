using Billing.Api.Http;
using Billing.Application.Abstractions;
using Billing.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Billing.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/staff")]
public class StaffController(IStaffService staff) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid? restaurantId, CancellationToken cancellationToken) =>
        ApiResults.From(await staff.GetAsync(restaurantId, cancellationToken));

    [HttpPost("employees")]
    public async Task<IActionResult> Hire([FromQuery] Guid? restaurantId, SaveEmployeeRequest request, CancellationToken cancellationToken) =>
        ApiResults.From(await staff.HireAsync(restaurantId, request, cancellationToken));

    [HttpPut("employees/{id:guid}")]
    public async Task<IActionResult> Update([FromQuery] Guid? restaurantId, Guid id, SaveEmployeeRequest request, CancellationToken cancellationToken) =>
        ApiResults.From(await staff.UpdateAsync(restaurantId, id, request, cancellationToken));

    [HttpDelete("employees/{id:guid}")]
    public async Task<IActionResult> Archive([FromQuery] Guid? restaurantId, Guid id, CancellationToken cancellationToken) =>
        ApiResults.From(await staff.ArchiveAsync(restaurantId, id, cancellationToken));

    [HttpPost("leave")]
    public async Task<IActionResult> Leave([FromQuery] Guid? restaurantId, SaveLeaveRequest request, CancellationToken cancellationToken) =>
        ApiResults.From(await staff.RequestLeaveAsync(restaurantId, request, cancellationToken));

    [HttpPost("leave/{id:guid}/decision")]
    public async Task<IActionResult> Decide([FromQuery] Guid? restaurantId, Guid id, DecideLeaveRequest request, CancellationToken cancellationToken) =>
        ApiResults.From(await staff.DecideLeaveAsync(restaurantId, id, request, cancellationToken));
}

[Authorize]
[ApiController]
[Route("api/attendance")]
public class AttendanceController(IAttendanceService attendance) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid? restaurantId, CancellationToken cancellationToken) =>
        ApiResults.From(await attendance.GetAsync(restaurantId, cancellationToken));

    [HttpPost("shifts")]
    public async Task<IActionResult> SaveShift([FromQuery] Guid? restaurantId, SaveShiftRequest request, CancellationToken cancellationToken) =>
        ApiResults.From(await attendance.SaveShiftAsync(restaurantId, request, cancellationToken));

    [HttpDelete("shifts/{id:guid}")]
    public async Task<IActionResult> ArchiveShift([FromQuery] Guid? restaurantId, Guid id, CancellationToken cancellationToken) =>
        ApiResults.From(await attendance.ArchiveShiftAsync(restaurantId, id, cancellationToken));

    [HttpPut("assignments")]
    public async Task<IActionResult> Assign([FromQuery] Guid? restaurantId, AssignShiftRequest request, CancellationToken cancellationToken) =>
        ApiResults.From(await attendance.AssignAsync(restaurantId, request, cancellationToken));

    [HttpPost("in")]
    public async Task<IActionResult> CheckIn([FromQuery] Guid? restaurantId, MarkAttendanceRequest request, CancellationToken cancellationToken) =>
        ApiResults.From(await attendance.CheckInAsync(restaurantId, request, cancellationToken));

    [HttpPost("out")]
    public async Task<IActionResult> CheckOut([FromQuery] Guid? restaurantId, MarkAttendanceRequest request, CancellationToken cancellationToken) =>
        ApiResults.From(await attendance.CheckOutAsync(restaurantId, request, cancellationToken));
}
