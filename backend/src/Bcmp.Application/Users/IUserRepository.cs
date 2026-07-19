using Bcmp.Domain.Users;

namespace Bcmp.Application.Users;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <param name="normalizedEmail">Must already be normalized via <see cref="User.NormalizeEmail"/>.</param>
    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<int> CountEnabledAdministratorsAsync(CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);

    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
}
