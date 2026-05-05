using System.Text.Json.Nodes;

namespace FormBuilder.Backend;

public sealed class FcaLookupService
{
    private readonly HttpClient                _http;
    private readonly IntegrationSettingsService _settings;
    private readonly ILogger<FcaLookupService>  _logger;

    public FcaLookupService(HttpClient http, IntegrationSettingsService settings, ILogger<FcaLookupService> logger)
    {
        _http     = http;
        _settings = settings;
        _logger   = logger;
    }

    public async Task<FcaFirmWithAddressResult?> LookupFirmWithAddress(string frn)
    {
        var firm = await LookupFirm(frn);
        if (firm is null) return null;

        var addresses    = await LookupFirmAddresses(frn);
        var tradingNames = await LookupFirmNames(frn);

        var principalAddr = addresses.FirstOrDefault(
            a => a.FcaType?.Equals("Principal Place of Business", StringComparison.OrdinalIgnoreCase) == true)
            ?? addresses.FirstOrDefault();

        return new FcaFirmWithAddressResult
        {
            Name                 = firm.Name,
            Status               = firm.Status,
            CompaniesHouseNumber = firm.CompaniesHouseNumber,
            Phone                = principalAddr?.Phone,
            Email                = principalAddr?.Email,
            Website              = principalAddr?.Website,
            Addresses            = addresses,
            TradingNames         = tradingNames
        };
    }

    public async Task<FcaFirmResult?> LookupFirm(string frn)
    {
        var (email, key) = GetCredentials();
        if (email is null) return null;

        foreach (var fmt in FrnFormats(frn))
        {
            var result = await TryLookupFirm(fmt, email, key!);
            if (result is not null) return result;
        }
        return null;
    }

    public async Task<List<FcaAddressResult>> LookupFirmAddresses(string frn)
    {
        var (email, key) = GetCredentials();
        if (email is null) return [];

        foreach (var fmt in FrnFormats(frn))
        {
            var addresses = await TryLookupAddresses(fmt, email, key!);
            if (addresses.Count > 0) return addresses;
        }
        return [];
    }

    public async Task<List<FcaTradingNameResult>> LookupFirmNames(string frn)
    {
        var (email, key) = GetCredentials();
        if (email is null) return [];

        foreach (var fmt in FrnFormats(frn))
        {
            var names = await TryLookupNames(fmt, email, key!);
            if (names.Count > 0) return names;
        }
        return [];
    }

    private (string? Email, string? Key) GetCredentials()
    {
        var email = _settings.GetSetting("FCA Register", "Email");
        var key   = _settings.GetSetting("FCA Register", "ApiKey");
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(key))
        {
            _logger.LogWarning("FCA API credentials not configured");
            return (null, null);
        }
        return (email, key);
    }

    private static IEnumerable<string> FrnFormats(string frn) =>
        new[] { frn, frn.PadLeft(6, '0'), frn.TrimStart('0') }.Distinct();

    private async Task<FcaFirmResult?> TryLookupFirm(string frn, string email, string key)
    {
        try
        {
            using var req  = BuildRequest($"https://register.fca.org.uk/services/V0.1/Firm/{Uri.EscapeDataString(frn)}", email, key);
            var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return null;

            var json = JsonNode.Parse(await resp.Content.ReadAsStringAsync());
            var data = json?["Data"];
            var firm = data is JsonArray arr && arr.Count > 0 ? arr[0] : data as JsonObject;
            if (firm is null) return null;

            return new FcaFirmResult
            {
                Name                 = firm["Organisation Name"]?.GetValue<string>() ?? string.Empty,
                Status               = firm["Status"]?.GetValue<string>() ?? string.Empty,
                CompaniesHouseNumber = firm["Companies House Number"]?.GetValue<string>(),
                Phone                = (firm["Phone Number"] ?? firm["Phone"] ?? firm["Telephone"] ?? firm["Tel"])
                                           ?.GetValue<string>()?.Trim(),
                Website              = (firm["Website"] ?? firm["Website URL"] ?? firm["Web Address"] ?? firm["URL"])
                                           ?.GetValue<string>()?.Trim()
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug("FCA firm lookup failed for {Frn}: {Msg}", frn, ex.Message);
            return null;
        }
    }

    private async Task<List<FcaAddressResult>> TryLookupAddresses(string frn, string email, string key)
    {
        try
        {
            using var req  = BuildRequest($"https://register.fca.org.uk/services/V0.1/Firm/{Uri.EscapeDataString(frn)}/Address", email, key);
            var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return [];

            var json = JsonNode.Parse(await resp.Content.ReadAsStringAsync());
            var data = (json?["Data"] ?? json?["data"]) as JsonArray;
            if (data is null || data.Count == 0) return [];

            return data
                .Where(i => i is not null)
                .Select(i => MapFcaAddress(i!))
                .Where(a => a is not null)
                .Cast<FcaAddressResult>()
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogDebug("FCA address lookup failed for {Frn}: {Msg}", frn, ex.Message);
            return [];
        }
    }

    private async Task<List<FcaTradingNameResult>> TryLookupNames(string frn, string email, string key)
    {
        try
        {
            using var req  = BuildRequest($"https://register.fca.org.uk/services/V0.1/Firm/{Uri.EscapeDataString(frn)}/Names", email, key);
            var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return [];

            var json = JsonNode.Parse(await resp.Content.ReadAsStringAsync());
            var data = (json?["Data"] ?? json?["data"]) as JsonArray;
            if (data is null || data.Count == 0) return [];

            var results = new List<FcaTradingNameResult>();
            foreach (var item in data)
            {
                if (item is null) continue;
                var currentNames = item["Current Names"] as JsonArray;
                if (currentNames is null) continue;

                foreach (var entry in currentNames)
                {
                    if (entry is null) continue;
                    var name = entry["Name"]?.GetValue<string>()?.Trim();
                    if (string.IsNullOrEmpty(name)) continue;

                    results.Add(new FcaTradingNameResult
                    {
                        Name          = name,
                        Status        = entry["Status"]?.GetValue<string>()?.Trim(),
                        EffectiveFrom = entry["Effective From"]?.GetValue<string>()?.Trim()
                    });
                }
            }
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogDebug("FCA names lookup failed for {Frn}: {Msg}", frn, ex.Message);
            return [];
        }
    }

    private static HttpRequestMessage BuildRequest(string url, string email, string key)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("X-Auth-Email", email);
        req.Headers.Add("X-Auth-Key", key);
        return req;
    }

    private static FcaAddressResult? MapFcaAddress(JsonNode item)
    {
        var line1   = item["Address Line 1"]?.GetValue<string>()?.Trim();
        var line2   = item["Address Line 2"]?.GetValue<string>()?.Trim();
        var town    = item["Town"]?.GetValue<string>()?.Trim();
        var county  = item["County"]?.GetValue<string>()?.Trim();
        var country = item["Country"]?.GetValue<string>()?.Trim();
        var post    = item["Postcode"]?.GetValue<string>()?.Trim();
        var fcaType = item["Type"]?.GetValue<string>()?.Trim();
        var phone   = (item["Phone Number"] ?? item["Phone"] ?? item["Telephone"] ?? item["Tel"] ?? item["Contact Number"])?.GetValue<string>()?.Trim();
        var email   = (item["Email"] ?? item["Email Address"] ?? item["Contact Email"])?.GetValue<string>()?.Trim();
        var website = (item["Website"] ?? item["Website Address"])?.GetValue<string>()?.Trim();

        string? buildingNumber = null, thoroughfareName = null;
        if (!string.IsNullOrEmpty(line1))
        {
            var parts = line1.Split(' ', 2);
            if (parts.Length == 2 && parts[0].All(c => char.IsDigit(c) || c == '-'))
            {
                buildingNumber   = parts[0];
                thoroughfareName = parts[1];
            }
            else
            {
                thoroughfareName = line1;
            }
        }

        var depLocality = !string.IsNullOrEmpty(line2)   ? line2
                        : !string.IsNullOrEmpty(county)  ? county
                        : null;

        return new FcaAddressResult
        {
            BuildingNumber    = buildingNumber,
            ThoroughfareName  = thoroughfareName,
            DependentLocality = depLocality,
            PostTown          = town,
            Postcode          = post,
            Country           = country,
            Phone             = phone,
            Email             = email,
            Website           = website,
            FcaType           = fcaType,
            AddressType       = MapAddressType(fcaType)
        };
    }

    private static string MapAddressType(string? fcaType) =>
        fcaType?.ToLowerInvariant() switch
        {
            "principal office" or "principal" => "Registered",
            "principal place of business"     => "Trading",
            "correspondence"                  => "Correspondence",
            "branch"                          => "Branch",
            "trading"                         => "Trading",
            "complaints"                      => "Complaints",
            _                                 => "Registered"
        };
}

public sealed class FcaFirmResult
{
    public string  Name                 { get; set; } = string.Empty;
    public string  Status               { get; set; } = string.Empty;
    public string? CompaniesHouseNumber { get; set; }
    public string? Phone                { get; set; }
    public string? Website              { get; set; }
}

public sealed class FcaAddressResult
{
    public string? BuildingNumber    { get; set; }
    public string? ThoroughfareName  { get; set; }
    public string? DependentLocality { get; set; }
    public string? PostTown          { get; set; }
    public string? Postcode          { get; set; }
    public string? Country           { get; set; }
    public string? Phone             { get; set; }
    public string? Email             { get; set; }
    public string? Website           { get; set; }
    public string? FcaType           { get; set; }
    public string  AddressType       { get; set; } = "Registered";
}

public sealed class FcaTradingNameResult
{
    public string  Name          { get; set; } = string.Empty;
    public string? Status        { get; set; }
    public string? EffectiveFrom { get; set; }
}

public sealed class FcaFirmWithAddressResult
{
    public string  Name                 { get; set; } = string.Empty;
    public string  Status               { get; set; } = string.Empty;
    public string? CompaniesHouseNumber { get; set; }
    public string? Phone                { get; set; }
    public string? Email                { get; set; }
    public string? Website              { get; set; }
    public List<FcaAddressResult>     Addresses    { get; set; } = [];
    public List<FcaTradingNameResult> TradingNames { get; set; } = [];
}
