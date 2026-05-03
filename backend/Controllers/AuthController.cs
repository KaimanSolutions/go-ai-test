using FormBuilder.Backend.Models;
using FormBuilder.Backend.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FormBuilder.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly JwtAuthService _jwtAuthService;
    private readonly ExternalAuthService _externalAuthService;

    public AuthController(JwtAuthService jwtAuthService, ExternalAuthService externalAuthService)
    {
        _jwtAuthService = jwtAuthService;
        _externalAuthService = externalAuthService;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Username and password are required.");

        var user = _externalAuthService.ValidateLocalCredentials(request.Username, request.Password);
        if (user is null)
            return Unauthorized();

        var token = _jwtAuthService.GenerateToken(user);
        return Ok(new AuthResponse { AccessToken = token, UserName = user.UserName, Email = user.Email });
    }

    [HttpGet("sso")]
    public IActionResult StartSSO()
    {
        var redirectUrl = Url.Action("HandleSSOCallback");
        return Challenge(new AuthenticationProperties { RedirectUri = redirectUrl ?? "/" }, OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> HandleSSOCallback()
    {
        var result = await HttpContext.AuthenticateAsync(OpenIdConnectDefaults.AuthenticationScheme);
        if (!result.Succeeded || result.Principal == null)
            return Unauthorized();

        var user = _externalAuthService.BuildUserFromClaims(result.Principal.Claims);
        var token = _jwtAuthService.GenerateToken(user);
        return Ok(new AuthResponse { AccessToken = token, UserName = user.UserName, Email = user.Email });
    }

    [Authorize]
    [HttpGet("profile")]
    public IActionResult Profile()
    {
        return Ok(new
        {
            Name = User.Identity?.Name,
            Claims = User.Claims.Select(c => new { c.Type, c.Value })
        });
    }
}
