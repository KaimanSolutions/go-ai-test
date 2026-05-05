using Microsoft.AspNetCore.Mvc;

namespace FormBuilder.Backend;

[ApiController]
[Route("api/epc")]
public sealed class EpcController : ControllerBase
{
    private readonly EpcService _epc;

    public EpcController(EpcService epc) => _epc = epc;

    [HttpGet("domestic/search")]
    public Task<IActionResult> DomesticSearch(
        [FromQuery(Name = "date_start")]        string?   dateStart        = null,
        [FromQuery(Name = "date_end")]          string?   dateEnd          = null,
        [FromQuery(Name = "postcode")]          string?   postcode         = null,
        [FromQuery(Name = "address")]           string?   address          = null,
        [FromQuery(Name = "uprn")]              long?     uprn             = null,
        [FromQuery(Name = "current_page")]      int?      currentPage      = null,
        [FromQuery(Name = "page_size")]         int?      pageSize         = null,
        [FromQuery(Name = "council")]           string[]? council          = null,
        [FromQuery(Name = "constituency")]      string[]? constituency     = null,
        [FromQuery(Name = "efficiency_rating")] string[]? efficiencyRating = null) =>
        ProxyAsync(_epc.SearchDomesticAsync(BuildParams(
            dateStart, dateEnd, postcode, address, uprn,
            currentPage, pageSize, council, constituency, efficiencyRating)));

    [HttpGet("non-domestic/search")]
    public Task<IActionResult> NonDomesticSearch(
        [FromQuery(Name = "date_start")]        string?   dateStart        = null,
        [FromQuery(Name = "date_end")]          string?   dateEnd          = null,
        [FromQuery(Name = "postcode")]          string?   postcode         = null,
        [FromQuery(Name = "address")]           string?   address          = null,
        [FromQuery(Name = "uprn")]              long?     uprn             = null,
        [FromQuery(Name = "current_page")]      int?      currentPage      = null,
        [FromQuery(Name = "page_size")]         int?      pageSize         = null,
        [FromQuery(Name = "council")]           string[]? council          = null,
        [FromQuery(Name = "constituency")]      string[]? constituency     = null,
        [FromQuery(Name = "efficiency_rating")] string[]? efficiencyRating = null) =>
        ProxyAsync(_epc.SearchNonDomesticAsync(BuildParams(
            dateStart, dateEnd, postcode, address, uprn,
            currentPage, pageSize, council, constituency, efficiencyRating)));

    [HttpGet("certificate")]
    public async Task<IActionResult> Certificate([FromQuery(Name = "certificate_number")] string? certificateNumber)
    {
        if (string.IsNullOrWhiteSpace(certificateNumber))
            return BadRequest(new { error = "certificate_number is required." });

        return await ProxyAsync(_epc.FetchCertificateAsync(certificateNumber));
    }

    private static EpcSearchParams BuildParams(
        string? dateStart, string? dateEnd, string? postcode, string? address, long? uprn,
        int? currentPage, int? pageSize, string[]? council, string[]? constituency, string[]? efficiencyRating) =>
        new()
        {
            DateStart        = dateStart,
            DateEnd          = dateEnd,
            Postcode         = postcode,
            Address          = address,
            Uprn             = uprn,
            CurrentPage      = currentPage,
            PageSize         = pageSize,
            Council          = council,
            Constituency     = constituency,
            EfficiencyRating = efficiencyRating
        };

    private static async Task<IActionResult> ProxyAsync(Task<EpcResult> task)
    {
        var result = await task;
        return new ObjectResult(result.Json) { StatusCode = result.StatusCode };
    }
}
