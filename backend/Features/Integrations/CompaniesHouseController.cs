using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FormBuilder.Backend;

[ApiController]
[Route("api/companies-house")]
[Authorize]
public sealed class CompaniesHouseController : ControllerBase
{
    private readonly CompaniesHouseService _service;

    public CompaniesHouseController(CompaniesHouseService service)
    {
        _service = service;
    }

    /// <summary>
    /// Returns the Companies House company profile for the given company number.
    /// </summary>
    [HttpGet("company/{companyNumber}")]
    public async Task<IActionResult> GetCompanyProfile(string companyNumber, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(companyNumber))
            return BadRequest("Company number is required.");

        var result = await _service.GetCompanyProfileAsync(companyNumber, ct);
        return StatusCode(result.StatusCode, result.Json);
    }
}
