using Bcmp.Domain.Users;

namespace Bcmp.Application.Users;

public sealed record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    UserRole Role,
    bool IsEnabled,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? LastLoginAtUtc)
{
    public static UserDto FromDomain(User user) => new(
        user.Id,
        user.Email,
        user.DisplayName,
        user.Role,
        user.IsEnabled,
        user.CreatedAtUtc,
        user.LastLoginAtUtc);
}
