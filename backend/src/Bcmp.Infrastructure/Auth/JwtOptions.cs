using System.ComponentModel.DataAnnotations;

namespace Bcmp.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Auth:Jwt";

    [Required, MinLength(32)]
    public required string SigningKey { get; init; }

    public string Issuer { get; init; } = "BodyCorporatePortal";

    public string Audience { get; init; } = "BodyCorporatePortal";

    public double ExpiryHours { get; init; } = 8;
}
