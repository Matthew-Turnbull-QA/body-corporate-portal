using Bcmp.Domain.Users;

namespace Bcmp.Application.Users;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <exception cref="InvalidOperationException">A user with this email already exists.</exception>
    Task<UserDto> AddUserAsync(
        string email,
        string displayName,
        UserRole role,
        Guid? createdByUserId,
        CancellationToken cancellationToken = default);

    /// <exception cref="KeyNotFoundException">No user with this id exists.</exception>
    /// <exception cref="InvalidOperationException">This would demote the last enabled Administrator.</exception>
    Task<UserDto> UpdateUserAsync(
        Guid id,
        string displayName,
        UserRole role,
        CancellationToken cancellationToken = default);

    /// <exception cref="KeyNotFoundException">No user with this id exists.</exception>
    Task EnableUserAsync(Guid id, CancellationToken cancellationToken = default);

    /// <exception cref="KeyNotFoundException">No user with this id exists.</exception>
    /// <exception cref="InvalidOperationException">This would disable the last enabled Administrator.</exception>
    Task DisableUserAsync(Guid id, CancellationToken cancellationToken = default);
}
