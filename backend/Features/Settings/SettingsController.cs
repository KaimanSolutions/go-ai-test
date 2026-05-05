using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FormBuilder.Backend;

[ApiController]
[Route("api/[controller]")]
public sealed class SettingsController : ControllerBase
{
    private readonly BrandingSettingsService _brandingSettingsService;

    public SettingsController(BrandingSettingsService brandingSettingsService) =>
        _brandingSettingsService = brandingSettingsService;

    [HttpGet("branding")]
    public IActionResult GetBranding() => Ok(_brandingSettingsService.GetSettings());

    [Authorize]
    [HttpPost("branding")]
    public IActionResult UpdateBranding([FromBody] BrandingSettings settings)
    {
        if (settings is null) return BadRequest("Branding settings are required.");
        return Ok(_brandingSettingsService.UpdateSettings(settings));
    }
}
