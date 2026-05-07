using FormBuilder.Backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;

namespace FormBuilder.Backend;

[ApiController]
[Route("api/notes")]
[Authorize]
public sealed class NoteController(NoteService service, FormBuilderDbContext db) : ControllerBase
{
    [HttpGet("application/{applicationId:int}")]
    public IActionResult GetForApplication(int applicationId)
        => Ok(service.GetForApplication(applicationId, CurrentUserRole));

    [HttpPost("application/{applicationId:int}")]
    public IActionResult Add(int applicationId, [FromBody] AddNoteRequest req)
    {
        // Resolve the current stage name from the application
        var app = db.Applications
            .Include(a => a.CurrentStage)
            .FirstOrDefault(a => a.Id == applicationId);

        if (app is null) return NotFound(new { error = "Application not found." });

        var (success, error, id) = service.Add(
            applicationId, req, CurrentUserName, CurrentUserRole, app.CurrentStage?.Name);

        return success ? Ok(new { id }) : BadRequest(new { error });
    }

    [HttpGet("my/recent")]
    public IActionResult GetMyRecentNotes()
    {
        var email = CurrentUserEmail;
        if (string.IsNullOrEmpty(email)) return Unauthorized();
        return Ok(service.GetMyRecentNotes(email, CurrentUserRole, CurrentUserName));
    }

    [HttpPatch("{id:int}/visibility")]
    [Authorize(Roles = UserRoles.Admin)]
    public IActionResult UpdateVisibility(int id, [FromBody] UpdateNoteVisibilityRequest req)
    {
        var (success, error) = service.UpdateVisibility(id, req.IsClientVisible, req.IsBrokerVisible);
        return success ? Ok() : NotFound(new { error });
    }

    private string CurrentUserRole =>
        User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
        ?? User.FindFirst("role")?.Value
        ?? UserRoles.Admin;

    private string CurrentUserName =>
        User.FindFirst("name")?.Value
        ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
        ?? User.Identity?.Name
        ?? "Unknown";

    private string CurrentUserEmail =>
        User.Claims.FirstOrDefault(c =>
            c.Type == System.Security.Claims.ClaimTypes.Email ||
            c.Type == JwtRegisteredClaimNames.Email)?.Value
        ?? string.Empty;
}
