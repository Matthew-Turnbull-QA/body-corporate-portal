namespace Bcmp.Domain.Users;

public sealed record User
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string DisplayName { get; init; }
    public required bool IsPortalAdmin { get; init; }
    public string? PasswordHash { get; init; }
    public bool IsEnabled { get; init; } = true;
    public required DateTimeOffset CreatedAtUtc { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public DateTimeOffset? LastLoginAtUtc { get; init; }

    public static User Create(
        Guid id,
        string email,
        string displayName,
        DateTimeOffset createdAtUtc,
        bool isPortalAdmin = false,
        Guid? createdByUserId = null,
        string? passwordHash = null)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be empty.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name cannot be empty.", nameof(displayName));
        }

        return new User
        {
            Id = id,
            Email = NormalizeEmail(email),
            DisplayName = displayName.Trim(),
            IsPortalAdmin = isPortalAdmin,
            PasswordHash = string.IsNullOrWhiteSpace(passwordHash) ? null : passwordHash,
            IsEnabled = true,
            CreatedAtUtc = createdAtUtc,
            CreatedByUserId = createdByUserId,
        };
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
