using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FormBuilder.Backend;

[ApiController]
[Route("api/[controller]")]
public sealed class FormBuilderController : ControllerBase
{
    private readonly FormSchemaService  _schemaService;
    private readonly RuleEngineService  _ruleEngineService;

    public FormBuilderController(FormSchemaService schemaService, RuleEngineService ruleEngineService)
    {
        _schemaService     = schemaService;
        _ruleEngineService = ruleEngineService;
    }

    [HttpGet("schemas")]
    public IActionResult GetSchemas() => Ok(_schemaService.GetAllSchemas());

    [Authorize]
    [HttpGet("schemas/archived")]
    public IActionResult GetArchivedSchemas() => Ok(_schemaService.GetArchivedSchemas());

    [HttpGet("schema/{id}")]
    public IActionResult GetSchema(string id)
    {
        var schema = _schemaService.GetSchema(id);
        if (schema is null) return NotFound($"Schema '{id}' not found.");
        return Ok(schema);
    }

    [Authorize]
    [HttpPost("schema")]
    public IActionResult SaveSchema([FromBody] FormSchema schema)
    {
        if (schema is null || string.IsNullOrWhiteSpace(schema.Id))
            return BadRequest("Schema ID is required.");

        _schemaService.SaveSchema(schema);
        return Ok(schema);
    }

    [Authorize]
    [HttpPatch("schema/{id}/workflow")]
    public IActionResult LinkWorkflow(string id, [FromBody] LinkWorkflowRequest request)
    {
        var (success, error) = _schemaService.LinkWorkflow(id, request.WorkflowId);
        if (!success) return BadRequest(error);
        return NoContent();
    }

    [Authorize]
    [HttpDelete("schema/{id}")]
    public IActionResult DeleteSchema(string id)
    {
        if (_schemaService.GetSchema(id) is null)
            return NotFound($"Schema '{id}' not found.");

        _schemaService.ArchiveSchema(id);
        return NoContent();
    }

    [Authorize]
    [HttpPost("schema/{id}/restore")]
    public IActionResult RestoreSchema(string id)
    {
        if (!_schemaService.UnarchiveSchema(id))
            return NotFound($"Archived schema '{id}' not found.");

        return NoContent();
    }

    [HttpPost("validate")]
    public IActionResult ValidateForm([FromBody] FormSubmissionRequest request)
    {
        if (request?.Schema is null)
            return BadRequest("Schema and submission values are required.");

        return Ok(_ruleEngineService.ValidateSubmission(request.Schema, request.Values));
    }
}
