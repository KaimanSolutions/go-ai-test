using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace FormBuilder.Backend;

[ApiController]
[Route("api/[controller]")]
public sealed class CompanyController : ControllerBase
{
    private readonly CompanyService     _companyService;
    private readonly FcaLookupService   _fcaLookupService;
    private readonly UserAccountService _userAccountService;

    public CompanyController(CompanyService companyService, FcaLookupService fcaLookupService, UserAccountService userAccountService)
    {
        _companyService     = companyService;
        _fcaLookupService   = fcaLookupService;
        _userAccountService = userAccountService;
    }

    [Authorize]
    [HttpGet("mine")]
    public IActionResult GetMine()
    {
        var email = User.Claims.FirstOrDefault(c =>
            c.Type == System.Security.Claims.ClaimTypes.Email ||
            c.Type == JwtRegisteredClaimNames.Email)?.Value;

        if (email is null) return Unauthorized();

        var user = _userAccountService.GetByEmail(email);
        if (user is null) return NotFound("User account not found.");
        if (user.CompanyId is null) return NotFound("You are not linked to a company.");

        var company = _companyService.GetDetail(user.CompanyId.Value);
        if (company is null) return NotFound("Company not found.");
        return Ok(company);
    }

    [HttpGet]
    public IActionResult GetAll() => Ok(_companyService.GetAll());

    [HttpGet("brokers")]
    public IActionResult GetBrokers([FromQuery] string? fca = null) =>
        Ok(_companyService.GetBrokers(fca));

    [HttpGet("networks")]
    public IActionResult GetNetworks() => Ok(_companyService.GetNetworks());

    [HttpGet("clubs")]
    public IActionResult GetClubs() => Ok(_companyService.GetClubs());

    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        var company = _companyService.GetDetail(id);
        if (company is null) return NotFound($"Company {id} not found.");
        return Ok(company);
    }

    [HttpGet("fca-lookup/{frn}")]
    public async Task<IActionResult> LookupByFrn(string frn)
    {
        if (string.IsNullOrWhiteSpace(frn))
            return BadRequest("FCA reference number is required.");

        var result = await _fcaLookupService.LookupFirmWithAddress(frn);
        if (result is null) return NotFound($"No FCA firm found for {frn}.");
        return Ok(result);
    }

    [HttpPost]
    public IActionResult Register([FromBody] CompanyRegistrationRequest request)
    {
        var (success, error, company) = _companyService.Register(request);
        if (!success) return BadRequest(error);
        return Ok(new { company!.Id, company.Name, company.FCANumber });
    }

    [HttpPost("{id}/bank-details")]
    public IActionResult AddBankDetails(int id, [FromBody] BankDetailsRequest request)
    {
        var (success, error) = _companyService.AddBankDetails(id, request);
        if (!success) return BadRequest(error);
        return NoContent();
    }

    [HttpDelete("{id}/bank-details/{bankDetailsId}")]
    public IActionResult DeleteBankDetails(int id, int bankDetailsId)
    {
        var (success, error) = _companyService.DeleteBankDetails(id, bankDetailsId);
        if (!success) return NotFound(error);
        return NoContent();
    }
}
