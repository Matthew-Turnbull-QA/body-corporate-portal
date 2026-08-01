using Bcmp.Domain.Assignments;
using Bcmp.Domain.Jobs;

namespace Bcmp.Application.Assignments;

public sealed record AssignmentRuleDto(
    Guid Id,
    string Name,
    bool IsEnabled,
    int Priority,
    Guid TargetTrusteeUserId,
    string TargetTrusteeName,
    Guid? PropertyId,
    string? PropertyName,
    JobSource? JobSource,
    IReadOnlyList<string> Keywords,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    public static AssignmentRuleDto FromDomain(
        AssignmentRule rule,
        string targetTrusteeName,
        string? propertyName) => new(
            rule.Id,
            rule.Name,
            rule.IsEnabled,
            rule.Priority,
            rule.TargetTrusteeUserId,
            targetTrusteeName,
            rule.PropertyId,
            propertyName,
            rule.JobSource,
            rule.GetKeywords(),
            rule.CreatedAtUtc,
            rule.UpdatedAtUtc);
}
