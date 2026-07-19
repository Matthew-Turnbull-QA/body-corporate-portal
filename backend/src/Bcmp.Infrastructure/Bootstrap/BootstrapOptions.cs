using System.ComponentModel.DataAnnotations;

namespace Bcmp.Infrastructure.Bootstrap;

public sealed class BootstrapOptions
{
    public const string SectionName = "Bootstrap";

    [Required, EmailAddress]
    public required string AdminEmail { get; init; }

    public string? AdminDisplayName { get; init; }
}
