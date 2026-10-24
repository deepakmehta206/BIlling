using Billing.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Billing.Api.Http;

public static class ApiResults
{
    public static IActionResult From<T>(ServiceResult<T> result) =>
        result.Succeeded
            ? new OkObjectResult(result.Value)
            : new ObjectResult(new { message = result.Error }) { StatusCode = result.StatusCode };
}
