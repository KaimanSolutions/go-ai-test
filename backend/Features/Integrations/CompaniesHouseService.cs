using System.Text;
using System.Text.Json.Nodes;

namespace FormBuilder.Backend;

public sealed class CompaniesHouseService
{
    private const string BaseUrl = "https://api.company-information.service.gov.uk";

    private readonly HttpClient                     _http;
    private readonly IntegrationSettingsService     _settings;
    private readonly ILogger<CompaniesHouseService> _logger;

    public CompaniesHouseService(
        HttpClient                     http,
        IntegrationSettingsService     settings,
        ILogger<CompaniesHouseService> logger)
    {
        _http     = http;
        _settings = settings;
        _logger   = logger;
    }

    public async Task<CompaniesHouseResult> GetCompanyProfileAsync(
        string            companyNumber,
        CancellationToken ct = default)
    {
        var apiKey = GetApiKey();
        if (apiKey is null)
            return CompaniesHouseResult.Fail(503, "Companies House API key is not configured.");

        // Company numbers are upper-cased and zero-padded to 8 chars by convention
        var normalised = companyNumber.Trim().ToUpperInvariant();
        var url        = $"{BaseUrl}/company/{Uri.EscapeDataString(normalised)}";

        return await SendAsync(url, apiKey, ct);
    }

    private async Task<CompaniesHouseResult> SendAsync(string url, string apiKey, CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);

            // Companies House uses HTTP Basic auth: API key as username, empty password
            var encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{apiKey}:"));
            req.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", encoded);

            var resp = await _http.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);

            JsonNode? json = null;
            try { json = JsonNode.Parse(body); } catch { /* non-JSON error body */ }

            return new CompaniesHouseResult((int)resp.StatusCode, json, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Companies House API request failed for {Url}", url);
            return CompaniesHouseResult.Fail(502, "Companies House API request failed.");
        }
    }

    private string? GetApiKey()
    {
        var key = _settings.GetSetting("Companies House", "ApiKey");
        if (string.IsNullOrWhiteSpace(key))
        {
            _logger.LogWarning("Companies House API key is not configured");
            return null;
        }
        return key;
    }
}

public sealed class CompaniesHouseResult
{
    public int       StatusCode { get; }
    public JsonNode? Json       { get; }
    public string    RawBody    { get; }

    public CompaniesHouseResult(int statusCode, JsonNode? json, string rawBody)
    {
        StatusCode = statusCode;
        Json       = json;
        RawBody    = rawBody;
    }

    public static CompaniesHouseResult Fail(int code, string message) =>
        new(code, JsonNode.Parse($"{{\"error\":\"{message}\"}}"), message);
}
