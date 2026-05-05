using FormBuilder.Backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FormBuilder.Backend;

[ApiController]
[Route("api/apilogs")]
[Authorize(Roles = UserRoles.Admin)]
public sealed class ApiLogsController : ControllerBase
{
    private readonly FormBuilderDbContext _db;

    public ApiLogsController(FormBuilderDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string? integration = null,
        [FromQuery] int page            = 1,
        [FromQuery] int pageSize        = 50)
    {
        pageSize = Math.Clamp(pageSize, 1, 200);
        page     = Math.Max(page, 1);

        var query = _db.ApiRequestLogs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(integration))
            query = query.Where(l => l.Integration == integration);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(l => l.RequestedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new
            {
                l.Id, l.Integration, l.Method, l.Endpoint,
                l.StatusCode, l.DurationMs, l.RequestedAt, l.ErrorMessage
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetLog(int id)
    {
        var log = await _db.ApiRequestLogs
            .AsNoTracking()
            .Where(l => l.Id == id)
            .Select(l => new
            {
                l.Id, l.Integration, l.Method, l.Endpoint,
                l.StatusCode, l.DurationMs, l.RequestedAt,
                l.RequestHeaders, l.RequestBody, l.ResponseBody, l.ErrorMessage
            })
            .FirstOrDefaultAsync();

        if (log is null) return NotFound();
        return Ok(log);
    }

    [HttpGet("integrations")]
    public async Task<IActionResult> GetIntegrations()
    {
        var list = await _db.ApiRequestLogs
            .AsNoTracking()
            .Select(l => l.Integration)
            .Distinct()
            .OrderBy(i => i)
            .ToListAsync();

        return Ok(list);
    }
}
