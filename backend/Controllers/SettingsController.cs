using FormBuilder.Backend.Models;
using FormBuilder.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FormBuilder.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly BrandingSettingsService _brandingSettingsService;

    public SettingsController(BrandingSettingsService brandingSettingsService)
    {
        _brandingSettingsService = brandingSettingsService;
    }

    [HttpGet("branding")]
    public IActionResult GetBranding()
    {
        return Ok(_brandingSettingsService.GetSettings());
    }

    [Authorize]
    [HttpPost("branding")]
    public IActionResult UpdateBranding([FromBody] BrandingSettings settings)
    {
        if (settings is null)
            return BadRequest("Branding settings are required.");

        var updated = _brandingSettingsService.UpdateSettings(settings);
        return Ok(updated);
    }
}
