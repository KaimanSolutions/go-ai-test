using FormBuilder.Backend.Data;

namespace FormBuilder.Backend;

public sealed class IntegrationSettingsService
{
    public static readonly IReadOnlyList<IntegrationDefinition> KnownIntegrations =
    [
        new IntegrationDefinition(
            Name:        "FCA Register",
            Description: "FCA firm register lookup for UK financial services authorisation and registration data.",
            Fields:
            [
                new CredentialField("Email",  "API Email", "email"),
                new CredentialField("ApiKey", "API Key",   "password")
            ]),

        new IntegrationDefinition(
            Name:        "EPB Data",
            Description: "Energy Performance of Buildings Data API for EPC certificate search and retrieval.",
            Fields:
            [
                new CredentialField("Token", "Bearer Token", "password")
            ]),

        new IntegrationDefinition(
            Name:        "Companies House",
            Description: "UK Companies House public data API for company profile and registration information.",
            Fields:
            [
                new CredentialField("ApiKey", "API Key", "password")
            ])
    ];

    private static readonly Dictionary<(string Integration, string Key), string> ConfigFallback = new()
    {
        [("FCA Register",    "Email")]  = "FcaApi:Email",
        [("FCA Register",    "ApiKey")] = "FcaApi:Key",
        [("EPB Data",        "Token")]  = "EpcApi:Token",
        [("Companies House", "ApiKey")] = "CompaniesHouse:ApiKey"
    };

    private readonly FormBuilderDbContext _context;
    private readonly IConfiguration       _config;

    public IntegrationSettingsService(FormBuilderDbContext context, IConfiguration config)
    {
        _context = context;
        _config  = config;
    }

    public string? GetSetting(string integration, string key)
    {
        var dbValue = _context.IntegrationSettings
            .FirstOrDefault(s => s.Integration == integration && s.Key == key)
            ?.Value;

        if (!string.IsNullOrWhiteSpace(dbValue)) return dbValue;

        if (ConfigFallback.TryGetValue((integration, key), out var configKey))
            return _config[configKey];

        return null;
    }

    public bool IsConfigured(string integration, string key) =>
        !string.IsNullOrWhiteSpace(GetSetting(integration, key));

    public void SetSetting(string integration, string key, string value)
    {
        var existing = _context.IntegrationSettings
            .FirstOrDefault(s => s.Integration == integration && s.Key == key);

        if (existing is not null)
        {
            existing.Value     = value;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _context.IntegrationSettings.Add(new IntegrationSetting
            {
                Integration = integration,
                Key         = key,
                Value       = value,
                UpdatedAt   = DateTime.UtcNow
            });
        }

        _context.SaveChanges();
    }
}

public sealed record IntegrationDefinition(
    string                         Name,
    string                         Description,
    IReadOnlyList<CredentialField> Fields);

public sealed record CredentialField(
    string Key,
    string Label,
    string Type);
