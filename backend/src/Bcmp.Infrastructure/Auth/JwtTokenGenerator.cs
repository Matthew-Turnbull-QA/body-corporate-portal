using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Bcmp.Application.Auth;
using Bcmp.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Bcmp.Infrastructure.Auth;

public sealed class JwtTokenGenerator(IOptions<JwtOptions> options) : IJwtTokenGenerator
{
    public string GenerateToken(User user)
    {
        var jwtOptions = options.Value;
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            // Plain "role" (not ClaimTypes.Role) so it survives JwtSecurityTokenHandler's default outbound
            // claim-type mapping unchanged and matches Program.cs's RoleClaimType on validation.
            new Claim("role", user.Role.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: jwtOptions.Issuer,
            audience: jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(jwtOptions.ExpiryHours),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
