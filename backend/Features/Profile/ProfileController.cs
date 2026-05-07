using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace FormBuilder.Backend;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class ProfileController : ControllerBase
{
    private readonly UserAccountService _userAccountService;

    public ProfileController(UserAccountService userAccountService) =>
        _userAccountService = userAccountService;

    private string? CurrentUserEmail =>
        User.Claims.FirstOrDefault(c =>
            c.Type == System.Security.Claims.ClaimTypes.Email ||
            c.Type == JwtRegisteredClaimNames.Email)?.Value;

    [HttpGet]
    public IActionResult GetProfile()
    {
        var email = CurrentUserEmail;
        if (email is null) return Unauthorized();

        var profile = _userAccountService.GetProfileByEmail(email);
        if (profile is null) return NotFound("User account not found.");
        return Ok(profile);
    }

    [HttpPut]
    public IActionResult UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var email = CurrentUserEmail;
        if (email is null) return Unauthorized();

        var (success, error, profile) = _userAccountService.UpdateProfileByEmail(email, request);
        if (!success) return BadRequest(error);
        return Ok(profile);
    }
}
