using Bcmp.Domain.Jobs;

namespace Bcmp.Application.Assignments;

public interface IAssignmentRuleService
{
    Task<IReadOnlyList<AssignmentRuleDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<AssignmentRuleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AssignmentRuleDto> CreateAsync(
        string name,
        Guid targetTrusteeUserId,
        Guid? propertyId,
        JobSource? jobSource,
        IReadOnlyList<string> keywords,
        CancellationToken cancellationToken = default);

    Task<AssignmentRuleDto> UpdateAsync(
        Guid id,
        string name,
        bool isEnabled,
        Guid targetTrusteeUserId,
        Guid? propertyId,
        JobSource? jobSource,
        IReadOnlyList<string> keywords,
        CancellationToken cancellationToken = default);

    Task<AssignmentRuleDto> SetEnabledAsync(Guid id, bool isEnabled, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AssignmentRuleDto>> ReorderAsync(
        IReadOnlyList<Guid> orderedRuleIds,
        CancellationToken cancellationToken = default);
}
