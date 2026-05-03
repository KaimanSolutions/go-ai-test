using FormBuilder.Backend.Models;
using Microsoft.AspNetCore.Hosting;
using System.Text.Json;

namespace FormBuilder.Backend.Services;

public sealed class BrandingSettingsService
{
    private readonly string _filePath;
    private readonly object _lock = new();
    private BrandingSettings _settings;

    public BrandingSettingsService(IWebHostEnvironment environment)
    {
        var dataFolder = Path.Combine(environment.ContentRootPath, "Data");
        Directory.CreateDirectory(dataFolder);
        _filePath = Path.Combine(dataFolder, "branding-settings.json");
        _settings = LoadSettings();
    }

    public BrandingSettings GetSettings()
    {
        lock (_lock)
        {
            return _settings;
        }
    }

    public BrandingSettings UpdateSettings(BrandingSettings settings)
    {
        lock (_lock)
        {
            _settings = ApplyDefaults(settings);
            SaveSettings(_settings);
            return _settings;
        }
    }

    private BrandingSettings LoadSettings()
    {
        if (!File.Exists(_filePath))
        {
            var defaults = GetDefaultSettings();
            SaveSettings(defaults);
            return defaults;
        }

        var json = File.ReadAllText(_filePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            var defaults = GetDefaultSettings();
            SaveSettings(defaults);
            return defaults;
        }

        try
        {
            var settings = JsonSerializer.Deserialize<BrandingSettings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return settings is not null ? ApplyDefaults(settings) : GetDefaultSettings();
        }
        catch
        {
            var defaults = GetDefaultSettings();
            SaveSettings(defaults);
            return defaults;
        }
    }

    private void SaveSettings(BrandingSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }

    private static BrandingSettings ApplyDefaults(BrandingSettings settings)
    {
        return new BrandingSettings
        {
            PrimaryColor = string.IsNullOrWhiteSpace(settings.PrimaryColor) ? "#0ea5e9" : settings.PrimaryColor,
            AccentColor = string.IsNullOrWhiteSpace(settings.AccentColor) ? "#7c3aed" : settings.AccentColor,
            BackgroundColor = string.IsNullOrWhiteSpace(settings.BackgroundColor) ? "#0f172a" : settings.BackgroundColor,
            SurfaceColor = string.IsNullOrWhiteSpace(settings.SurfaceColor) ? "#111827" : settings.SurfaceColor,
            CardColor = string.IsNullOrWhiteSpace(settings.CardColor) ? "#1f2937" : settings.CardColor,
            TextColor = string.IsNullOrWhiteSpace(settings.TextColor) ? "#e2e8f0" : settings.TextColor,
            MutedTextColor = string.IsNullOrWhiteSpace(settings.MutedTextColor) ? "#94a3b8" : settings.MutedTextColor,
            FontBody = string.IsNullOrWhiteSpace(settings.FontBody) ? "Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif" : settings.FontBody,
            FontHeading = string.IsNullOrWhiteSpace(settings.FontHeading) ? "Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif" : settings.FontHeading
        };
    }

    private static BrandingSettings GetDefaultSettings() => new();
}
