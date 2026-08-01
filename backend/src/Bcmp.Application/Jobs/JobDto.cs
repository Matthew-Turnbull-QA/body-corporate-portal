using Bcmp.Domain.Jobs;
using Bcmp.Domain.Assignments;

namespace Bcmp.Application.Jobs;

public sealed record JobDto(
    Guid Id,
    string JobNumber,
    Guid? PropertyId,
    string? PropertyName,
    string Title,
    string Description,
    JobStatus Status,
    JobSource Source,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    Guid? AssignedTrusteeUserId,
    string? AssignedTrusteeName,
    AssignmentSource? AssignmentSource,
    Guid? AssignmentRuleId,
    string? AssignmentRuleName)
{
    public static JobDto FromDomain(
        Job job,
        string? propertyName,
        string? assignedTrusteeName = null,
        string? assignmentRuleName = null) => new(
        job.Id,
        job.JobNumber,
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
        job.AssignedTrusteeUserId is null ? null : assignedTrusteeName,
        job.AssignmentSource,
        job.AssignmentRuleId,
        job.AssignmentRuleId is null ? null : assignmentRuleName);
}
