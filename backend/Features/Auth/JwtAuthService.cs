using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FormBuilder.Backend;

public sealed class JwtAuthService
{
    private readonly SymmetricSecurityKey _key;
    private readonly string _issuer;
    private readonly string _audience;

    public JwtAuthService(IConfiguration configuration)
    {
        var section = configuration.GetSection("JwtSettings");
        _key      = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(section["Secret"] ?? string.Empty));
        _issuer   = section["Issuer"]   ?? string.Empty;
        _audience = section["Audience"] ?? string.Empty;
    }

    public string GenerateToken(UserProfile user)
    {
        var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.UserName),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("name",                        user.UserName),
            new Claim(ClaimTypes.Role,               user.Role)
        };

        var token = new JwtSecurityToken(
            issuer:             _issuer,
            audience:           _audience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddHours(6),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
