using Bcmp.Domain.AccessRequests;
using Bcmp.Domain.Users;

namespace Bcmp.Application.AccessRequests;

public interface IAccessRequestService
{
    Task<AccessRequestDto> SubmitAsync(
        string email,
        string displayName,
        string phoneNumber,
        string propertyOrUnit,
        AccessRequestRelationship relationship,
        string? message,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccessRequestDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<AccessRequestDto> ApproveAsync(
        Guid id,
        UserRole role,
        IReadOnlyCollection<UserPermission>? permissions,
        string? password,
        Guid reviewedByUserId,
        string? reviewNote,
        CancellationToken cancellationToken = default);

    Task<AccessRequestDto> RejectAsync(
        Guid id,
        Guid reviewedByUserId,
        string? reviewNote,
        CancellationToken cancellationToken = default);
}
