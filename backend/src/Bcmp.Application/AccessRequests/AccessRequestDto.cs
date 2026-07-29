using Bcmp.Domain.AccessRequests;

namespace Bcmp.Application.AccessRequests;

public sealed record AccessRequestDto(
    Guid Id,
    string Email,
    string DisplayName,
    string PhoneNumber,
    string PropertyOrUnit,
    AccessRequestRelationship Relationship,
    string Message,
    AccessRequestStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReviewedAtUtc,
    Guid? ReviewedByUserId,
    Guid? ExistingUserId,
    bool? ExistingUserIsEnabled,
    Guid? ApprovedUserId,
    string? ReviewNote)
{
    public static AccessRequestDto FromDomain(AccessRequest request, bool? existingUserIsEnabled = null) => new(
        request.Id,
        request.Email,
        request.DisplayName,
        request.PhoneNumber,
        request.PropertyOrUnit,
        request.Relationship,
        request.Message,
        request.Status,
        request.CreatedAtUtc,
        request.ReviewedAtUtc,
        request.ReviewedByUserId,
        request.ExistingUserId,
        existingUserIsEnabled,
        request.ApprovedUserId,
        request.ReviewNote);
}
