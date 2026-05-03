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

    [Authorize]
    [HttpPost("schema")]
    public IActionResult SaveSchema([FromBody] FormSchema schema)
    {
        if (schema is null || string.IsNullOrWhiteSpace(schema.Id))
            return BadRequest("Schema ID is required.");

        _schemaService.SaveSchema(schema);
        return Ok(schema);
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
