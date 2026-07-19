namespace Bcmp.Domain.Users;

public sealed record User
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string DisplayName { get; init; }
    public required UserRole Role { get; init; }
    public bool IsEnabled { get; init; } = true;
    public required DateTimeOffset CreatedAtUtc { get; init; }
    public Guid? CreatedByUserId { get; init; }
    public DateTimeOffset? LastLoginAtUtc { get; init; }

    public static User Create(
        Guid id,
        string email,
        string displayName,
        UserRole role,
        DateTimeOffset createdAtUtc,
        Guid? createdByUserId = null)
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
            Role = role,
            IsEnabled = true,
            CreatedAtUtc = createdAtUtc,
            CreatedByUserId = createdByUserId,
        };
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
