using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FormBuilder.Backend;

[ApiController]
[Route("api/login-events")]
[Authorize(Roles = UserRoles.Admin)]
public sealed class LoginEventController(LoginEventService service) : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll(
        [FromQuery] int     page       = 1,
        [FromQuery] int     pageSize   = 50,
        [FromQuery] string? userType   = null,
        [FromQuery] string? deviceType = null,
        [FromQuery] string? authMethod = null,
        [FromQuery] string? from       = null,
        [FromQuery] string? to         = null)
    {
        DateTimeOffset? fromDate = from is not null && DateTimeOffset.TryParse(from, out var f) ? f : null;
        DateTimeOffset? toDate   = to   is not null && DateTimeOffset.TryParse(to,   out var t) ? t : null;

        var (items, total) = service.GetAll(page, pageSize, userType, deviceType, authMethod, fromDate, toDate);
        return Ok(new { items, total, page, pageSize });
    }

    [HttpGet("summary")]
    public IActionResult GetSummary() => Ok(service.GetSummary());
}
