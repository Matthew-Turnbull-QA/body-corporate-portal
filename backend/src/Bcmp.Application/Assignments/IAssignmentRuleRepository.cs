using Bcmp.Domain.Assignments;

namespace Bcmp.Application.Assignments;

public interface IAssignmentRuleRepository
{
    Task<AssignmentRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AssignmentRule>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(AssignmentRule rule, CancellationToken cancellationToken = default);

    Task UpdateAsync(AssignmentRule rule, CancellationToken cancellationToken = default);

    Task UpdateManyAsync(IReadOnlyList<AssignmentRule> rules, CancellationToken cancellationToken = default);
}
