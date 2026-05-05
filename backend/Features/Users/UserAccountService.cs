using FormBuilder.Backend.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace FormBuilder.Backend;

public sealed class UserAccountService
{
    private readonly FormBuilderDbContext _context;

    public UserAccountService(FormBuilderDbContext context) => _context = context;

    public UserAccount? ValidateCredentials(string emailOrUsername, string password)
    {
        var user = _context.UserAccounts
            .Include(u => u.Company)
            .FirstOrDefault(u => u.Email == emailOrUsername || u.Username == emailOrUsername);

        if (user is null) return null;
        return VerifyPassword(password, user.PasswordHash) ? user : null;
    }

    public (bool Success, string Error) Register(RegistrationRequest request)
    {
        if (request.Role != UserRoles.Client && request.Role != UserRoles.Broker)
            return (false, "Invalid role. Must be Client or Broker.");

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return (false, "Email and password are required.");

        if (request.Password.Length < 8)
            return (false, "Password must be at least 8 characters.");

        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            return (false, "First and last name are required.");

        if (_context.UserAccounts.Any(u => u.Email == request.Email))
            return (false, "An account with this email already exists.");

        if (request.Role == UserRoles.Broker)
        {
            if (!request.CompanyId.HasValue)
                return (false, "Please select a company.");

            if (!_context.Companies.Any(c => c.Id == request.CompanyId.Value))
                return (false, "Selected company does not exist.");
        }

        _context.UserAccounts.Add(new UserAccount
        {
            Username      = request.Email,
            Email         = request.Email,
            PasswordHash  = HashPassword(request.Password),
            Role          = request.Role,
            FirstName     = request.FirstName,
            LastName      = request.LastName,
            Phone         = request.Phone ?? string.Empty,
            LicenseNumber = request.LicenseNumber,
            CompanyId     = request.CompanyId,
            CreatedAt     = DateTime.UtcNow
        });

        _context.SaveChanges();
        return (true, string.Empty);
    }

    public UserProfile ToProfile(UserAccount account) => new()
    {
        Id            = account.Id,
        UserName      = account.Email,
        Email         = account.Email,
        Role          = account.Role,
        FirstName     = account.FirstName,
        LastName      = account.LastName,
        Phone         = string.IsNullOrEmpty(account.Phone) ? null : account.Phone,
        LicenseNumber = account.LicenseNumber,
        CompanyId     = account.CompanyId,
        CompanyName   = account.Company?.Name,
        CreatedAt     = account.CreatedAt
    };

    public Dictionary<string, List<UserProfile>> GetUsersGroupedByRole()
    {
        var users = _context.UserAccounts
            .Include(u => u.Company)
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .Select(u => new UserProfile
            {
                Id            = u.Id,
                UserName      = u.Email,
                Email         = u.Email,
                Role          = u.Role,
                FirstName     = u.FirstName,
                LastName      = u.LastName,
                Phone         = string.IsNullOrEmpty(u.Phone) ? null : u.Phone,
                LicenseNumber = u.LicenseNumber,
                CompanyId     = u.CompanyId,
                CompanyName   = u.Company != null ? u.Company.Name : null,
                CreatedAt     = u.CreatedAt
            })
            .ToList();

        return users
            .GroupBy(u => u.Role)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 350_000, HashAlgorithmName.SHA512, 64);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split(':');
        if (parts.Length != 2) return false;
        try
        {
            var salt     = Convert.FromBase64String(parts[0]);
            var hash     = Convert.FromBase64String(parts[1]);
            var testHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 350_000, HashAlgorithmName.SHA512, 64);
            return CryptographicOperations.FixedTimeEquals(hash, testHash);
        }
        catch { return false; }
    }
}
