using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AgroIdentity.Api.Domain;
using Microsoft.IdentityModel.Tokens;

namespace AgroIdentity.Api.Services;

public class JwtTokenService(IConfiguration config)
{
    public (string token, DateTimeOffset expiresAt) Generate(User user)
    {
        var issuer = config["Jwt:Issuer"]!;
        var audience = config["Jwt:Audience"]!;
        var key = config["Jwt:Key"]!;
        var expiresMinutes = int.Parse(config["Jwt:ExpiresMinutes"] ?? "480");

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(expiresMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email)
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt.UtcDateTime,
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(jwt), expiresAt);
    }
}