using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FormBuilder.Backend;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly JwtAuthService      _jwtAuthService;
    private readonly ExternalAuthService _externalAuthService;
    private readonly UserAccountService  _userAccountService;

    public AuthController(
        JwtAuthService      jwtAuthService,
        ExternalAuthService externalAuthService,
        UserAccountService  userAccountService)
    {
        _jwtAuthService      = jwtAuthService;
        _externalAuthService = externalAuthService;
        _userAccountService  = userAccountService;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Username and password are required.");

        var adminUser = _externalAuthService.ValidateLocalCredentials(request.Username, request.Password);
        if (adminUser is not null)
        {
            var token = _jwtAuthService.GenerateToken(adminUser);
            return Ok(new AuthResponse { AccessToken = token, UserName = adminUser.UserName, Email = adminUser.Email, Role = adminUser.Role });
        }

        var dbUser = _userAccountService.ValidateCredentials(request.Username, request.Password);
        if (dbUser is not null)
        {
            var profile = _userAccountService.ToProfile(dbUser);
            var token   = _jwtAuthService.GenerateToken(profile);
            return Ok(new AuthResponse { AccessToken = token, UserName = profile.UserName, Email = profile.Email, Role = profile.Role, CompanyId = profile.CompanyId, CompanyName = profile.CompanyName });
        }

        return Unauthorized("Invalid credentials.");
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] RegistrationRequest request)
    {
        var (success, error) = _userAccountService.Register(request);
        if (!success) return BadRequest(error);
        return Ok(new { message = "Registration successful." });
    }

    [HttpGet("sso")]
    public IActionResult StartSSO()
    {
        var redirectUrl = Url.Action("HandleSSOCallback");
        return Challenge(
            new AuthenticationProperties { RedirectUri = redirectUrl ?? "/" },
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> HandleSSOCallback()
    {
        var result = await HttpContext.AuthenticateAsync(OpenIdConnectDefaults.AuthenticationScheme);
        if (!result.Succeeded || result.Principal is null) return Unauthorized();

        var user  = _externalAuthService.BuildUserFromClaims(result.Principal.Claims);
        var token = _jwtAuthService.GenerateToken(user);
        return Ok(new AuthResponse { AccessToken = token, UserName = user.UserName, Email = user.Email, Role = user.Role });
    }

    [Authorize]
    [HttpGet("profile")]
    public IActionResult Profile()
    {
        return Ok(new
        {
            Name   = User.Identity?.Name,
            Role   = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value,
            Claims = User.Claims.Select(c => new { c.Type, c.Value })
        });
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpGet("users")]
    public IActionResult GetUsers() => Ok(_userAccountService.GetUsersGroupedByRole());

    [Authorize(Roles = UserRoles.Admin)]
    [HttpGet("users/{id}")]
    public IActionResult GetUser(int id)
    {
        var user = _userAccountService.GetById(id);
        if (user is null) return NotFound($"User {id} not found.");
        return Ok(user);
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPost("users")]
    public IActionResult CreateUser([FromBody] RegistrationRequest request)
    {
        var (success, error) = _userAccountService.AdminCreateUser(request);
        if (!success) return BadRequest(error);
        return Ok(new { message = $"{request.Role} account created." });
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpDelete("users/{id}")]
    public IActionResult DeleteUser(int id)
    {
        var (success, error) = _userAccountService.Delete(id);
        if (!success) return NotFound(error);
        return NoContent();
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPatch("users/{id}/lockout")]
    public IActionResult SetLockout(int id, [FromBody] LockoutRequest request)
    {
        var (success, error) = _userAccountService.SetLockout(id, request.Locked);
        if (!success) return NotFound(error);
        return NoContent();
    }
}
