using Bcmp.Domain.Users;

namespace Bcmp.Application.Users;

public sealed record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    UserRole Role,
    IReadOnlyList<UserPermission> Permissions,
    bool HasLocalPassword,
    bool IsEnabled,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastLoginAtUtc)
{
    public static UserDto FromDomain(User user) => new(
        user.Id,
        user.Email,
        user.DisplayName,
        user.Role,
        ExpandPermissions(user.Permissions),
        user.PasswordHash is not null,
        user.IsEnabled,
        user.CreatedAtUtc,
        user.LastLoginAtUtc);

    private static IReadOnlyList<UserPermission> ExpandPermissions(UserPermission permissions) =>
        Enum.GetValues<UserPermission>()
            .Where(permission => permission != UserPermission.None && permissions.HasFlag(permission))
            .ToList();
}
