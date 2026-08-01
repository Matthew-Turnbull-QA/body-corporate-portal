using Bcmp.Application.Assignments;
using Bcmp.Domain.Assignments;
using Microsoft.EntityFrameworkCore;

namespace Bcmp.Infrastructure.Persistence;

public sealed class AssignmentRuleRepository(AppDbContext dbContext) : IAssignmentRuleRepository
{
    public async Task<AssignmentRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.AssignmentRules.FirstOrDefaultAsync(rule => rule.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AssignmentRule>> GetAllAsync(CancellationToken cancellationToken = default)
        => await dbContext.AssignmentRules
            .OrderBy(rule => rule.Priority)
            .ThenBy(rule => rule.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(AssignmentRule rule, CancellationToken cancellationToken = default)
    {
        dbContext.AssignmentRules.Add(rule);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AssignmentRule rule, CancellationToken cancellationToken = default)
    {
        TrackUpdatedRule(rule);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateManyAsync(IReadOnlyList<AssignmentRule> rules, CancellationToken cancellationToken = default)
    {
        foreach (var rule in rules)
        {
            TrackUpdatedRule(rule);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private void TrackUpdatedRule(AssignmentRule rule)
    {
        var trackedEntry = dbContext.ChangeTracker
            .Entries<AssignmentRule>()
            .FirstOrDefault(entry => entry.Entity.Id == rule.Id);

        if (trackedEntry is not null && !ReferenceEquals(trackedEntry.Entity, rule))
        {
            dbContext.Entry(trackedEntry.Entity).State = EntityState.Detached;
        }

        dbContext.AssignmentRules.Update(rule);
    }
}
