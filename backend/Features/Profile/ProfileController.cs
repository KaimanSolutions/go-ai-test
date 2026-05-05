using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FormBuilder.Backend;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class ProfileController : ControllerBase
{
    private readonly ProfileSettingsService _profileService;

    public ProfileController(ProfileSettingsService profileService) =>
        _profileService = profileService;

    [HttpGet]
    public IActionResult GetProfile() => Ok(_profileService.GetProfile());

    [HttpPut]
    public IActionResult UpdateProfile([FromBody] ProfileSettings profile)
    {
        if (profile is null) return BadRequest("Profile data is required.");
        return Ok(_profileService.UpdateProfile(profile));
    }
}
