using System.ComponentModel.DataAnnotations;

namespace Bcmp.Infrastructure.Auth;

public sealed class GoogleAuthOptions
{
    public const string SectionName = "Auth:Google";

    [Required]
    public required string ClientId { get; init; }
}
