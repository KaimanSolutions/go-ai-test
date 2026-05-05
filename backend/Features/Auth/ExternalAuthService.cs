using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FormBuilder.Backend;

public sealed class ExternalAuthService
{
    private readonly string _adminUsername;
    private readonly string _adminPassword;
    private readonly string _adminEmail;

    public ExternalAuthService(IConfiguration configuration)
    {
        var section    = configuration.GetSection("AdminAccount");
        _adminUsername = section["Username"] ?? "admin";
        _adminPassword = section["Password"] ?? string.Empty;
        _adminEmail    = section["Email"]    ?? "admin@example.com";
    }

    public UserProfile? ValidateLocalCredentials(string username, string password)
    {
        if (string.IsNullOrEmpty(_adminPassword)) return null;

        if (!username.Equals(_adminUsername, StringComparison.OrdinalIgnoreCase) || password != _adminPassword)
            return null;

        return new UserProfile
        {
            UserName = _adminUsername,
            Email    = _adminEmail,
            Role     = UserRoles.Admin
        };
    }

    public UserProfile BuildUserFromClaims(IEnumerable<Claim> claims)
    {
        return new UserProfile
        {
            UserName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
                    ?? claims.FirstOrDefault(c => c.Type == "name")?.Value
                    ?? "sso-user",
            Email    = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value
                    ?? claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value
                    ?? string.Empty,
            Role     = UserRoles.Client
        };
    }
}
