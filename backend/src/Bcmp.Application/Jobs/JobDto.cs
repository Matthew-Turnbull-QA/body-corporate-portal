using Bcmp.Domain.Jobs;

namespace Bcmp.Application.Jobs;

public sealed record JobDto(
    Guid Id,
    Guid PropertyId,
    string PropertyName,
    string Title,
    string Description,
    JobStatus Status,
    JobSource Source,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    Guid? AssignedTrusteeUserId,
    string? AssignedTrusteeName)
{
    public static JobDto FromDomain(Job job, string propertyName, string? assignedTrusteeName = null) => new(
        job.Id,
        job.PropertyId,
        propertyName,
        job.Title,
        job.Description,
        job.Status,
        job.Source,
        job.CreatedByUserId,
        job.CreatedAtUtc,
        job.UpdatedAtUtc,
        job.AssignedTrusteeUserId,
        job.AssignedTrusteeUserId is null ? null : assignedTrusteeName);
}
