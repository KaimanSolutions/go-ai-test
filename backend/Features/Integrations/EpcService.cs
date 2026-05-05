using System.Text.Json.Nodes;
using System.Web;

namespace FormBuilder.Backend;

public sealed class EpcService
{
    private const string BaseUrl = "https://api.get-energy-performance-data.communities.gov.uk";

    private readonly HttpClient                 _http;
    private readonly IntegrationSettingsService _settings;
    private readonly ILogger<EpcService>        _logger;

    public EpcService(HttpClient http, IntegrationSettingsService settings, ILogger<EpcService> logger)
    {
        _http     = http;
        _settings = settings;
        _logger   = logger;
    }

    public Task<EpcResult> SearchDomesticAsync(EpcSearchParams p, CancellationToken ct = default) =>
        SearchAsync("/api/domestic/search", p, ct);

    public Task<EpcResult> SearchNonDomesticAsync(EpcSearchParams p, CancellationToken ct = default) =>
        SearchAsync("/api/non-domestic/search", p, ct);

    public async Task<EpcResult> FetchCertificateAsync(string certificateNumber, CancellationToken ct = default)
    {
        var token = GetToken();
        if (token is null) return EpcResult.Fail(503, "EPC API token is not configured.");

        var qs = HttpUtility.ParseQueryString(string.Empty);
        qs["certificate_number"] = certificateNumber;
        return await SendAsync($"{BaseUrl}/api/certificate?{qs}", token, ct);
    }

    private async Task<EpcResult> SearchAsync(string path, EpcSearchParams p, CancellationToken ct)
    {
        var token = GetToken();
        if (token is null) return EpcResult.Fail(503, "EPC API token is not configured.");

        var qs = HttpUtility.ParseQueryString(string.Empty);

        if (!string.IsNullOrWhiteSpace(p.DateStart))  qs["date_start"]   = p.DateStart;
        if (!string.IsNullOrWhiteSpace(p.DateEnd))    qs["date_end"]     = p.DateEnd;
        if (!string.IsNullOrWhiteSpace(p.Postcode))   qs["postcode"]     = p.Postcode;
        if (!string.IsNullOrWhiteSpace(p.Address))    qs["address"]      = p.Address;
        if (p.Uprn.HasValue)                           qs["uprn"]         = p.Uprn.Value.ToString();
        if (p.CurrentPage.HasValue)                    qs["current_page"] = p.CurrentPage.Value.ToString();
        if (p.PageSize.HasValue)                       qs["page_size"]    = p.PageSize.Value.ToString();

        var url = AppendArray(AppendArray(AppendArray(
            $"{BaseUrl}{path}?{qs}",
            "council[]",          p.Council),
            "constituency[]",     p.Constituency),
            "efficiency_rating[]", p.EfficiencyRating);

        return await SendAsync(url, token, ct);
    }

    private async Task<EpcResult> SendAsync(string url, string token, CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var resp = await _http.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);

            JsonNode? json = null;
            try { json = JsonNode.Parse(body); } catch { /* plain-text error body */ }

            return new EpcResult((int)resp.StatusCode, json, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EPC API request failed for {Url}", url);
            return EpcResult.Fail(502, "EPC API request failed.");
        }
    }

    private string? GetToken()
    {
        var token = _settings.GetSetting("EPB Data", "Token");
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("EPB Data Token is not configured");
            return null;
        }
        return token;
    }

    private static string AppendArray(string url, string key, IEnumerable<string>? values)
    {
        if (values is null) return url;
        var encoded = values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => $"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(v)}");
        var joined = string.Join("&", encoded);
        return string.IsNullOrEmpty(joined) ? url : $"{url}&{joined}";
    }
}

public sealed class EpcSearchParams
{
    public string?   DateStart        { get; init; }
    public string?   DateEnd          { get; init; }
    public string?   Postcode         { get; init; }
    public string?   Address          { get; init; }
    public long?     Uprn             { get; init; }
    public int?      CurrentPage      { get; init; }
    public int?      PageSize         { get; init; }
    public string[]? Council          { get; init; }
    public string[]? Constituency     { get; init; }
    public string[]? EfficiencyRating { get; init; }
}

public sealed class EpcResult
{
    public int       StatusCode { get; }
    public JsonNode? Json       { get; }
    public string    RawBody    { get; }

    public EpcResult(int statusCode, JsonNode? json, string rawBody)
    {
        StatusCode = statusCode;
        Json       = json;
        RawBody    = rawBody;
    }

    public static EpcResult Fail(int code, string message) =>
        new(code, JsonNode.Parse($"{{\"error\":\"{message}\"}}"), message);
}
