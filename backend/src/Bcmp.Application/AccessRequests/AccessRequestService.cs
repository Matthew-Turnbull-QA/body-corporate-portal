using Bcmp.Application.Auth;
using Bcmp.Application.Users;
using Bcmp.Domain.AccessRequests;
using Bcmp.Domain.Users;

namespace Bcmp.Application.AccessRequests;

public sealed class AccessRequestService(
    IAccessRequestRepository accessRequestRepository,
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider) : IAccessRequestService
{
    public async Task<AccessRequestDto> SubmitAsync(
        string email,
        string displayName,
        string phoneNumber,
        string propertyOrUnit,
        AccessRequestRelationship relationship,
        string? message,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = User.NormalizeEmail(email);
        var existingPending = await accessRequestRepository.GetPendingByEmailAsync(normalizedEmail, cancellationToken);
        if (existingPending is not null)
        {
            throw new InvalidOperationException("An access request for this email is already pending.");
        }

        var existingUser = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        var request = AccessRequest.Create(
            Guid.NewGuid(),
            email,
            displayName,
            phoneNumber,
            propertyOrUnit,
            relationship,
            message,
            timeProvider.GetUtcNow(),
            existingUser?.Id);

        await accessRequestRepository.AddAsync(request, cancellationToken);
        return AccessRequestDto.FromDomain(request, existingUser?.IsEnabled);
    }

    public async Task<IReadOnlyList<AccessRequestDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var requests = await accessRequestRepository.GetAllAsync(cancellationToken);
        var users = await userRepository.GetAllAsync(cancellationToken);
        var userEnabledById = users.ToDictionary(user => user.Id, user => user.IsEnabled);

        return requests
            .Select(request => AccessRequestDto.FromDomain(
                request,
                request.ExistingUserId is Guid userId ? userEnabledById.GetValueOrDefault(userId) : null))
            .ToList();
    }

    public async Task<AccessRequestDto> ApproveAsync(
        Guid id,
        UserRole role,
        IReadOnlyCollection<UserPermission>? permissions,
        string? password,
        Guid reviewedByUserId,
        string? reviewNote,
        CancellationToken cancellationToken = default)
    {
        var request = await accessRequestRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Access request '{id}' was not found.");

        if (request.Status != AccessRequestStatus.Pending)
        {
            throw new InvalidOperationException("Only pending access requests can be approved.");
        }

        var now = timeProvider.GetUtcNow();
        var existingUser = request.ExistingUserId is Guid existingUserId
            ? await userRepository.GetByIdAsync(existingUserId, cancellationToken)
            : await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        var passwordHash = string.IsNullOrWhiteSpace(password) ? null : HashPassword(password);
        var combinedPermissions = CombinePermissions(permissions, role);
        User approvedUser;

        if (existingUser is null)
        {
            approvedUser = User.Create(
                Guid.NewGuid(),
                request.Email,
                request.DisplayName,
                role,
                now,
                reviewedByUserId,
                combinedPermissions,
                passwordHash);
            await userRepository.AddAsync(approvedUser, cancellationToken);
        }
        else
        {
            approvedUser = existingUser with
            {
                DisplayName = request.DisplayName,
                Role = role,
                Permissions = combinedPermissions,
                PasswordHash = passwordHash ?? existingUser.PasswordHash,
                IsEnabled = true,
            };
            await userRepository.UpdateAsync(approvedUser, cancellationToken);
        }

        var approved = request with
        {
            Status = AccessRequestStatus.Approved,
            ReviewedAtUtc = now,
            ReviewedByUserId = reviewedByUserId,
            ApprovedUserId = approvedUser.Id,
            ReviewNote = string.IsNullOrWhiteSpace(reviewNote) ? null : reviewNote.Trim(),
        };

        await accessRequestRepository.UpdateAsync(approved, cancellationToken);
        return AccessRequestDto.FromDomain(approved);
    }

    public async Task<AccessRequestDto> RejectAsync(
        Guid id,
        Guid reviewedByUserId,
        string? reviewNote,
        CancellationToken cancellationToken = default)
    {
        var request = await accessRequestRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Access request '{id}' was not found.");

        if (request.Status != AccessRequestStatus.Pending)
        {
            throw new InvalidOperationException("Only pending access requests can be rejected.");
        }

        var rejected = request with
        {
            Status = AccessRequestStatus.Rejected,
            ReviewedAtUtc = timeProvider.GetUtcNow(),
            ReviewedByUserId = reviewedByUserId,
            ReviewNote = string.IsNullOrWhiteSpace(reviewNote) ? null : reviewNote.Trim(),
        };

        await accessRequestRepository.UpdateAsync(rejected, cancellationToken);
        return AccessRequestDto.FromDomain(rejected);
    }

    private string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            throw new ArgumentException("Local passwords must be at least 8 characters long.", nameof(password));
        }

        return passwordHasher.HashPassword(password);
    }

    private static UserPermission CombinePermissions(IReadOnlyCollection<UserPermission>? permissions, UserRole role)
    {
        if (permissions is null)
        {
            return User.DefaultPermissionsFor(role);
        }

        return permissions.Aggregate(UserPermission.None, (current, permission) => current | permission);
    }
}
