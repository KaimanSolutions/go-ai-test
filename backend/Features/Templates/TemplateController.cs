using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FormBuilder.Backend;

[ApiController]
[Route("api/templates")]
[Authorize(Roles = UserRoles.Admin)]
public sealed class TemplateController(TemplateService service) : ControllerBase
{
    [HttpGet]
    public IActionResult GetAll([FromQuery] string? type = null)
        => Ok(service.GetAll(type));

    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        var t = service.GetById(id);
        return t is null ? NotFound() : Ok(t);
    }

    [HttpPost]
    public IActionResult Create([FromBody] TemplateSaveRequest req)
    {
        var (success, error, id) = service.Save(null, req);
        return success ? Ok(new { id }) : BadRequest(new { error });
    }

    [HttpPut("{id:int}")]
    public IActionResult Update(int id, [FromBody] TemplateSaveRequest req)
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

    [HttpPost("compile-mjml")]
    public IActionResult CompileMjml([FromBody] MjmlCompileRequest req)
    {
        var (success, result) = service.CompileMjml(req.Mjml);
        return success
            ? Ok(new { html = result })
            : BadRequest(new { error = result });
    }
}
