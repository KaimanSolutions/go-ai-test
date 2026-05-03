using FormBuilder.Backend.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FormBuilder.Backend.Services;

public sealed class ExternalAuthService
{
    public UserProfile? ValidateLocalCredentials(string username, string password)
    {
        if (username == "admin" && password == "Password123!")
        {
            return new UserProfile
            {
                UserName = username,
                Email = "admin@mortgage.example.com"
            };
        }

        return null;
    }

    public UserProfile BuildUserFromClaims(IEnumerable<Claim> claims)
    {
        return new UserProfile
        {
            UserName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? claims.FirstOrDefault(c => c.Type == "name")?.Value ?? "sso-user",
            Email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value ?? string.Empty
        };
    }
}
