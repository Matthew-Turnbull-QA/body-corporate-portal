using Bcmp.Domain.Users;

namespace Bcmp.Application.Users;

public sealed record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsPortalAdmin,
    bool IsSystem,
    bool HasLocalPassword,
    bool IsEnabled,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastLoginAtUtc)
{
    public static UserDto FromDomain(User user) => new(
        user.Id,
        user.Email,
        user.DisplayName,
        user.IsPortalAdmin,
        user.IsSystem,
        user.PasswordHash is not null,
        user.IsEnabled,
        user.CreatedAtUtc,
        user.LastLoginAtUtc);
}
