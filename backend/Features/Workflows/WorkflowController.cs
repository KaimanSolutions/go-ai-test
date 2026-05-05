using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FormBuilder.Backend;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = UserRoles.Admin)]
public sealed class WorkflowController : ControllerBase
{
    private readonly WorkflowService _service;

    public WorkflowController(WorkflowService service) => _service = service;

    [HttpGet]
    public IActionResult GetAll() => Ok(_service.GetAll());

    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        var workflow = _service.GetDetail(id);
        if (workflow is null) return NotFound($"Workflow {id} not found.");
        return Ok(workflow);
    }

    [HttpPost]
    public IActionResult Create([FromBody] WorkflowSaveRequest request)
    {
        var (success, error, id) = _service.Save(null, request);
        if (!success) return BadRequest(error);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:int}")]
    public IActionResult Update(int id, [FromBody] WorkflowSaveRequest request)
    {
        var (success, error, newId) = _service.Save(id, request);
        if (!success) return BadRequest(error);
        return Ok(new { id = newId });
    }

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        var (success, error) = _service.Delete(id);
        if (!success) return NotFound(error);
        return NoContent();
    }
}
