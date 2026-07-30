using Bcmp.Domain.AccessRequests;

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
        bool isPortalAdmin,
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
