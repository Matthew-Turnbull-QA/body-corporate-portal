using Bcmp.Domain.Users;

namespace Bcmp.Application.Users;

public sealed class UserService(IUserRepository userRepository, TimeProvider timeProvider) : IUserService
{
    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);
        return users.Select(UserDto.FromDomain).ToList();
    }

    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(id, cancellationToken);
        return user is null ? null : UserDto.FromDomain(user);
    }

    public async Task<UserDto> AddUserAsync(
        string email,
        string displayName,
        UserRole role,
        Guid? createdByUserId,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = User.NormalizeEmail(email);
        var existing = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException($"A user with email '{normalizedEmail}' already exists.");
        }

        var user = User.Create(Guid.NewGuid(), email, displayName, role, timeProvider.GetUtcNow(), createdByUserId);
        await userRepository.AddAsync(user, cancellationToken);
        return UserDto.FromDomain(user);
    }

    public async Task<UserDto> UpdateUserAsync(
        Guid id,
        string displayName,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"User '{id}' was not found.");

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name cannot be empty.", nameof(displayName));
        }

        if (user.Role == UserRole.Administrator && user.IsEnabled && role != UserRole.Administrator)
        {
            await EnsureNotLastEnabledAdministratorAsync(cancellationToken);
        }

        var updated = user with { DisplayName = displayName.Trim(), Role = role };
        await userRepository.UpdateAsync(updated, cancellationToken);
        return UserDto.FromDomain(updated);
    }

    public async Task EnableUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"User '{id}' was not found.");

        if (user.IsEnabled)
        {
            return;
        }

        await userRepository.UpdateAsync(user with { IsEnabled = true }, cancellationToken);
    }

    public async Task DisableUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"User '{id}' was not found.");

        if (!user.IsEnabled)
        {
            return;
        }

        if (user.Role == UserRole.Administrator)
        {
            await EnsureNotLastEnabledAdministratorAsync(cancellationToken);
        }

        await userRepository.UpdateAsync(user with { IsEnabled = false }, cancellationToken);
    }

    private async Task EnsureNotLastEnabledAdministratorAsync(CancellationToken cancellationToken)
    {
        var enabledAdminCount = await userRepository.CountEnabledAdministratorsAsync(cancellationToken);
        if (enabledAdminCount <= 1)
        {
            throw new InvalidOperationException("Cannot disable or demote the last enabled Administrator.");
        }
    }
}
