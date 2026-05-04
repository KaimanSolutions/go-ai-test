using FormBuilder.Backend.Models;
using FormBuilder.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FormBuilder.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FormBuilderController : ControllerBase
{
    private readonly FormSchemaService _schemaService;
    private readonly RuleEngineService _ruleEngineService;

    public FormBuilderController(FormSchemaService schemaService, RuleEngineService ruleEngineService)
    {
        _schemaService = schemaService;
        _ruleEngineService = ruleEngineService;
    }

    [HttpGet("schemas")]
    public IActionResult GetSchemas()
    {
        return Ok(_schemaService.GetAllSchemas());
    }

    [HttpGet("schema/{id}")]
    public IActionResult GetSchema(string id)
    {
        var schema = _schemaService.GetSchema(id);
        if (schema is null)
            return NotFound($"Schema '{id}' not found.");
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
    [HttpDelete("schema/{id}")]
    public IActionResult DeleteSchema(string id)
    {
        var existing = _schemaService.GetSchema(id);
        if (existing is null)
            return NotFound($"Schema '{id}' not found.");

        _schemaService.DeleteSchema(id);
        return NoContent();
    }

    [HttpPost("validate")]
    public IActionResult ValidateForm([FromBody] FormSubmissionRequest request)
    {
        if (request is null || request.Schema is null)
            return BadRequest("Schema and submission values are required.");

        var result = _ruleEngineService.ValidateSubmission(request.Schema, request.Values);
        return Ok(result);
    }
}
