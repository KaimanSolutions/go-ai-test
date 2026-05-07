using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace FormBuilder.Backend;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ApplicationController : ControllerBase
{
    private readonly ApplicationService _applicationService;

    public ApplicationController(ApplicationService applicationService)
    {
        _applicationService = applicationService;
    }

    // ASP.NET Core's JWT middleware maps "email" → ClaimTypes.Email URI by default,
    // so check both the mapped and unmapped forms.
    private string? CurrentUserEmail =>
        User.Claims.FirstOrDefault(c =>
            c.Type == System.Security.Claims.ClaimTypes.Email ||
            c.Type == JwtRegisteredClaimNames.Email)?.Value;

    private string CurrentUserRole =>
        User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value
        ?? string.Empty;

    [HttpGet]
    public IActionResult GetAll()
    {
        var email = CurrentUserEmail;
        if (email is null) return Unauthorized();
        return Ok(_applicationService.GetAll(email, CurrentUserRole));
    }

    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        var email = CurrentUserEmail;
        if (email is null) return Unauthorized();
        var app = _applicationService.GetById(id, email, CurrentUserRole);
        if (app is null) return NotFound($"Application {id} not found.");
        return Ok(app);
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateApplicationRequest request)
    {
        var email = CurrentUserEmail;
        if (email is null) return Unauthorized();
        var (success, error, app) = _applicationService.Create(email, CurrentUserRole, request);
        if (!success) return BadRequest(error);
        return Ok(app);
    }

    [HttpPut("{id}")]
    public IActionResult Update(int id, [FromBody] UpdateApplicationRequest request)
    {
        var email = CurrentUserEmail;
        if (email is null) return Unauthorized();
        var (success, error, app) = _applicationService.Update(id, email, CurrentUserRole, request);
        if (!success) return BadRequest(error);
        return Ok(app);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = UserRoles.Admin)]
    public IActionResult Delete(int id)
    {
        var (success, error) = _applicationService.Delete(id);
        if (!success) return NotFound(error);
        return NoContent();
    }
}
