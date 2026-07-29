namespace Bcmp.Domain.Users;

public sealed record User
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string DisplayName { get; init; }
    public required UserRole Role { get; init; }
    public required UserPermission Permissions { get; init; }
    public string? PasswordHash { get; init; }
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
        Guid? createdByUserId = null,
        UserPermission? permissions = null,
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
            Role = role,
            Permissions = permissions ?? DefaultPermissionsFor(role),
            PasswordHash = string.IsNullOrWhiteSpace(passwordHash) ? null : passwordHash,
            IsEnabled = true,
            CreatedAtUtc = createdAtUtc,
            CreatedByUserId = createdByUserId,
        };
    }

    public bool HasPermission(UserPermission permission) => (Permissions & permission) == permission;

    public static UserPermission DefaultPermissionsFor(UserRole role) => role switch
    {
        UserRole.Administrator => UserPermission.LoadJobs
            | UserPermission.CreateJobs
            | UserPermission.UpdateJobStatus
            | UserPermission.AssignJobs,
        UserRole.Trustee => UserPermission.LoadJobs
            | UserPermission.CreateJobs
            | UserPermission.UpdateJobStatus,
        _ => UserPermission.None,
    };

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
