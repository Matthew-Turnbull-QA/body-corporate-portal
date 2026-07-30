using Bcmp.Application.Auth;
using Bcmp.Domain.Users;

namespace Bcmp.Application.Users;

public sealed class UserService(IUserRepository userRepository, IPasswordHasher passwordHasher, TimeProvider timeProvider) : IUserService
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
        bool isPortalAdmin,
        string? password,
        Guid? createdByUserId,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = User.NormalizeEmail(email);
        var existing = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException($"A user with email '{normalizedEmail}' already exists.");
        }

        var passwordHash = string.IsNullOrWhiteSpace(password) ? null : HashPassword(password);
        var user = User.Create(
            Guid.NewGuid(),
            email,
            displayName,
            timeProvider.GetUtcNow(),
            isPortalAdmin,
            createdByUserId,
            passwordHash);
        await userRepository.AddAsync(user, cancellationToken);
        return UserDto.FromDomain(user);
    }

    public async Task<UserDto> UpdateUserAsync(
        Guid id,
        string displayName,
        bool isPortalAdmin,
        string? newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"User '{id}' was not found.");

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name cannot be empty.", nameof(displayName));
        }

        if (user.IsPortalAdmin && user.IsEnabled && !isPortalAdmin)
        {
            await EnsureNotLastEnabledPortalAdminAsync(cancellationToken);
        }

        var updated = user with
        {
            DisplayName = displayName.Trim(),
            IsPortalAdmin = isPortalAdmin,
            PasswordHash = string.IsNullOrWhiteSpace(newPassword) ? user.PasswordHash : HashPassword(newPassword),
        };
        await userRepository.UpdateAsync(updated, cancellationToken);
        return UserDto.FromDomain(updated);
    }

    public async Task<IReadOnlyList<UserDto>> GetAssignableTrusteesAsync(CancellationToken cancellationToken = default)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);
        return users
            .Where(user => user.IsEnabled)
            .Select(UserDto.FromDomain)
            .ToList();
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

        if (user.IsPortalAdmin)
        {
            await EnsureNotLastEnabledPortalAdminAsync(cancellationToken);
        }

        await userRepository.UpdateAsync(user with { IsEnabled = false }, cancellationToken);
    }

    private async Task EnsureNotLastEnabledPortalAdminAsync(CancellationToken cancellationToken)
    {
        var enabledAdminCount = await userRepository.CountEnabledPortalAdminsAsync(cancellationToken);
        if (enabledAdminCount <= 1)
        {
            throw new InvalidOperationException("Cannot disable or remove admin access from the last enabled portal admin.");
        }
    }

    private string HashPassword(string password)
    {
        ValidatePassword(password);
        return passwordHasher.HashPassword(password);
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            throw new ArgumentException("Local passwords must be at least 8 characters long.", nameof(password));
        }
    }

}
