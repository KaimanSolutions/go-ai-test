using FormBuilder.Backend.Models;
using FormBuilder.Backend.Data;

namespace FormBuilder.Backend.Services;

public sealed class BrandingSettingsService
{
    private readonly FormBuilderDbContext _context;

    public BrandingSettingsService(FormBuilderDbContext context)
    {
        _context = context;
    }

    public void EnsureDefaultSettings()
    {
        if (_context.BrandingSettings.Any())
            return;

        _context.BrandingSettings.Add(new BrandingSettings());
        _context.SaveChanges();
    }

    public BrandingSettings GetSettings()
    {
        return _context.BrandingSettings.OrderBy(b => b.Id).FirstOrDefault() ?? new BrandingSettings();
    }

    public BrandingSettings UpdateSettings(BrandingSettings settings)
    {
        var existing = _context.BrandingSettings.OrderBy(b => b.Id).FirstOrDefault();

        if (existing != null)
        {
            existing.PrimaryColor = settings.PrimaryColor;
            existing.AccentColor = settings.AccentColor;
            existing.BackgroundColor = settings.BackgroundColor;
            existing.SurfaceColor = settings.SurfaceColor;
            existing.CardColor = settings.CardColor;
            existing.TextColor = settings.TextColor;
            existing.MutedTextColor = settings.MutedTextColor;
            existing.FontBody = settings.FontBody;
            existing.FontHeading = settings.FontHeading;
            _context.SaveChanges();
            return existing;
        }

        _context.BrandingSettings.Add(settings);
        _context.SaveChanges();
        return settings;
    }
}
