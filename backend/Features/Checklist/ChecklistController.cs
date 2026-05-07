using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FormBuilder.Backend;

[ApiController]
[Route("api/checklist")]
[Authorize]
public sealed class ChecklistController(ChecklistService service) : ControllerBase
{
    // ── Template CRUD ─────────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult GetAll([FromQuery] string? formSchemaId = null)
        => Ok(service.GetAll(formSchemaId));

    [HttpPost]
    public IActionResult Create([FromBody] ChecklistItemRequest req)
    {
        var (success, error, id) = service.Save(null, req);
        return success ? Ok(new { id }) : BadRequest(new { error });
    }

    [HttpPut("{id:int}")]
    public IActionResult Update(int id, [FromBody] ChecklistItemRequest req)
    {
        var (success, error, _) = service.Save(id, req);
        return success ? Ok(new { id }) : BadRequest(new { error });
    }

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        var (success, error) = service.Delete(id);
        return success ? Ok() : NotFound(new { error });
    }

    // ── Application checklist ─────────────────────────────────────────────────

    [HttpGet("application/{applicationId:int}")]
    public IActionResult GetForApplication(int applicationId)
        => Ok(service.GetForApplication(applicationId, CurrentUserRole));

    [HttpPost("application/{applicationId:int}/generate")]
    public IActionResult Generate(int applicationId)
    {
        var (success, error) = service.GenerateForApplication(applicationId);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpPut("application/item/{id:int}/respond")]
    public IActionResult Respond(int id, [FromBody] ChecklistRespondRequest req)
    {
        var (success, error) = service.Respond(id, req.Text ?? string.Empty);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpPost("application/item/{id:int}/upload")]
    public async Task<IActionResult> Upload(
        int id, IFormFile file, [FromServices] IWebHostEnvironment env)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file provided." });

        var uploadDir = Path.Combine(env.ContentRootPath, "uploads", "checklist");
        Directory.CreateDirectory(uploadDir);

        var ext        = Path.GetExtension(file.FileName);
        var storedName = $"{id}_{Guid.NewGuid():N}{ext}";
        var fullPath   = Path.Combine(uploadDir, storedName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        var (success, error) = service.RecordUpload(id, file.FileName, storedName, file.ContentType);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpPut("application/item/{id:int}/status")]
    [Authorize(Roles = UserRoles.Admin)]
    public IActionResult UpdateStatus(int id, [FromBody] ChecklistStatusRequest req)
    {
        var (success, error) = service.UpdateStatus(id, req.Status);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpPost("application/item/{id:int}/comment")]
    public IActionResult AddComment(int id, [FromBody] ChecklistCommentRequest req)
    {
        var authorName = User.FindFirst("name")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
            ?? User.Identity?.Name
            ?? "Unknown";

        var (success, error) = service.AddComment(id, req.Comment, authorName);
        return success ? Ok() : BadRequest(new { error });
    }

    [HttpGet("application/item/{id:int}/download")]
    public IActionResult Download(int id, [FromServices] IWebHostEnvironment env)
    {
        var item = service.GetAppItem(id);
        if (item?.DocumentPath is null) return NotFound();

        var fullPath = Path.Combine(env.ContentRootPath, "uploads", "checklist", item.DocumentPath);
        if (!System.IO.File.Exists(fullPath)) return NotFound();

        var stream = System.IO.File.OpenRead(fullPath);
        return File(stream, item.DocumentContentType ?? "application/octet-stream", item.DocumentName ?? "document");
    }

    private string CurrentUserRole =>
        User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
        ?? User.FindFirst("role")?.Value
        ?? string.Empty;
}
