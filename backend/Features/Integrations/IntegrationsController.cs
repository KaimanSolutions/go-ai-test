using FormBuilder.Backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FormBuilder.Backend;

[ApiController]
[Route("api/integrations")]
[Authorize(Roles = UserRoles.Admin)]
public sealed class IntegrationsController : ControllerBase
{
    private readonly IntegrationSettingsService _settings;
    private readonly FormBuilderDbContext        _db;

    public IntegrationsController(IntegrationSettingsService settings, FormBuilderDbContext db)
    {
        _settings = settings;
        _db       = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetServices()
    {
        var activityMap = await _db.ApiRequestLogs
            .AsNoTracking()
            .GroupBy(l => l.Integration)
            .Select(g => new { Integration = g.Key, LastActivity = g.Max(l => l.RequestedAt) })
            .ToDictionaryAsync(x => x.Integration, x => x.LastActivity);

        var result = IntegrationSettingsService.KnownIntegrations.Select(def =>
        {
            var fields = def.Fields.Select(f => new
            {
                f.Key,
                f.Label,
                f.Type,
                Configured = _settings.IsConfigured(def.Name, f.Key)
            }).ToList();

            activityMap.TryGetValue(def.Name, out var lastActivity);

            return new
            {
                def.Name,
                def.Description,
                Configured   = fields.All(f => f.Configured),
                LastActivity = lastActivity == default(DateTime) ? (DateTime?)null : lastActivity,
                Fields       = fields
            };
        });

        return Ok(result);
    }

    [HttpPut("{integration}/credentials")]
    public IActionResult UpdateCredentials(string integration, [FromBody] Dictionary<string, string> credentials)
    {
        var def = IntegrationSettingsService.KnownIntegrations
            .FirstOrDefault(d => d.Name.Equals(integration, StringComparison.OrdinalIgnoreCase));

        if (def is null) return NotFound($"Integration '{integration}' not found.");

        var updated = 0;
        foreach (var field in def.Fields)
        {
            if (credentials.TryGetValue(field.Key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                _settings.SetSetting(def.Name, field.Key, value.Trim());
                updated++;
            }
        }

        if (updated == 0) return BadRequest("No valid credential fields provided.");
        return Ok(new { updated });
    }
}
