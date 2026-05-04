using FormBuilder.Backend.Models;
using FormBuilder.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FormBuilder.Backend.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    private readonly ProfileSettingsService _profileService;

    public ProfileController(ProfileSettingsService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet]
    public IActionResult GetProfile()
    {
        return Ok(_profileService.GetProfile());
    }

    [HttpPut]
    public IActionResult UpdateProfile([FromBody] ProfileSettings profile)
    {
        if (profile is null)
            return BadRequest("Profile data is required.");

        var updated = _profileService.UpdateProfile(profile);
        return Ok(updated);
    }
}
