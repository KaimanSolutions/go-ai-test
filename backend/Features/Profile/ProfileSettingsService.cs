using FormBuilder.Backend.Data;

namespace FormBuilder.Backend;

public sealed class ProfileSettingsService
{
    private readonly FormBuilderDbContext _context;

    public ProfileSettingsService(FormBuilderDbContext context) => _context = context;

    public void EnsureDefaultProfile()
    {
        if (_context.ProfileSettings.Any()) return;
        _context.ProfileSettings.Add(new ProfileSettings { DisplayName = "Admin" });
        _context.SaveChanges();
    }

    public ProfileSettings GetProfile() =>
        _context.ProfileSettings.OrderBy(p => p.Id).FirstOrDefault() ?? new ProfileSettings();

    public ProfileSettings UpdateProfile(ProfileSettings profile)
    {
        var existing = _context.ProfileSettings.OrderBy(p => p.Id).FirstOrDefault();

        if (existing is not null)
        {
            existing.DisplayName = profile.DisplayName;
            existing.JobTitle    = profile.JobTitle;
            existing.Department  = profile.Department;
            _context.SaveChanges();
            return existing;
        }

        _context.ProfileSettings.Add(profile);
        _context.SaveChanges();
        return profile;
    }
}
