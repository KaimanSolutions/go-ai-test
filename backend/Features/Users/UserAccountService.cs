using FormBuilder.Backend.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace FormBuilder.Backend;

public sealed class UserAccountService
{
    private readonly FormBuilderDbContext _context;

    public UserAccountService(FormBuilderDbContext context) => _context = context;

    public UserAccount? GetByEmail(string email) =>
        _context.UserAccounts.FirstOrDefault(u => u.Email == email);

    public UserProfile? GetById(int id)
    {
        var account = _context.UserAccounts
            .Include(u => u.Company)
            .FirstOrDefault(u => u.Id == id);
        return account is null ? null : ToProfile(account);
    }

    public UserAccount? ValidateCredentials(string emailOrUsername, string password)
    {
        var user = _context.UserAccounts
            .Include(u => u.Company)
            .FirstOrDefault(u => u.Email == emailOrUsername || u.Username == emailOrUsername);

        if (user is null) return null;
        if (user.IsLockedOut) return null;
        return VerifyPassword(password, user.PasswordHash) ? user : null;
    }

    public (bool Success, string Error) SetLockout(int id, bool locked)
    {
        var user = _context.UserAccounts.Find(id);
        if (user is null) return (false, "User not found.");
        user.IsLockedOut = locked;
        _context.SaveChanges();
        return (true, string.Empty);
    }

    // Admin-only: creates any role including Admin
    public (bool Success, string Error) AdminCreateUser(RegistrationRequest request)
    {
        var validRoles = new[] { UserRoles.Admin, UserRoles.Broker, UserRoles.Client };
        if (!validRoles.Contains(request.Role))
            return (false, "Role must be Admin, Broker or Client.");

        return CreateAccount(request, requireCompany: request.Role == UserRoles.Broker);
    }

    public (bool Success, string Error) Delete(int id)
    {
        var user = _context.UserAccounts.Find(id);
        if (user is null) return (false, "User not found.");
        _context.UserAccounts.Remove(user);
        _context.SaveChanges();
        return (true, string.Empty);
    }

    public (bool Success, string Error) Register(RegistrationRequest request)
    {
        if (request.Role != UserRoles.Client && request.Role != UserRoles.Broker)
            return (false, "Invalid role. Must be Client or Broker.");

        return CreateAccount(request, requireCompany: request.Role == UserRoles.Broker);
    }

    private (bool Success, string Error) CreateAccount(RegistrationRequest request, bool requireCompany)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return (false, "Email and password are required.");

        if (request.Password.Length < 8)
            return (false, "Password must be at least 8 characters.");

        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            return (false, "First and last name are required.");

        if (_context.UserAccounts.Any(u => u.Email == request.Email))
            return (false, "An account with this email already exists.");

        if (requireCompany)
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
        JobTitle      = account.JobTitle,
        Department    = account.Department,
        CompanyId     = account.CompanyId,
        CompanyName   = account.Company?.Name,
        IsLockedOut   = account.IsLockedOut,
        CreatedAt     = account.CreatedAt
    };

    public UserProfile? GetProfileByEmail(string email)
    {
        var account = _context.UserAccounts
            .Include(u => u.Company)
            .FirstOrDefault(u => u.Email == email);
        return account is null ? null : ToProfile(account);
    }

    public (bool Success, string Error, UserProfile? Profile) UpdateProfileByEmail(
        string email, UpdateProfileRequest request)
    {
        var account = _context.UserAccounts.FirstOrDefault(u => u.Email == email);
        if (account is null) return (false, "User account not found.", null);

        if (!string.IsNullOrWhiteSpace(request.FirstName)) account.FirstName  = request.FirstName.Trim();
        if (!string.IsNullOrWhiteSpace(request.LastName))  account.LastName   = request.LastName.Trim();
        if (request.Phone      is not null) account.Phone      = request.Phone.Trim();
        if (request.JobTitle   is not null) account.JobTitle   = request.JobTitle.Trim();
        if (request.Department is not null) account.Department = request.Department.Trim();

        _context.SaveChanges();
        return (true, string.Empty, ToProfile(account));
    }

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
                JobTitle      = u.JobTitle,
                Department    = u.Department,
                CompanyId     = u.CompanyId,
                CompanyName   = u.Company != null ? u.Company.Name : null,
                IsLockedOut   = u.IsLockedOut,
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
