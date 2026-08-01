using Bcmp.Application.Properties;
using Bcmp.Application.Users;
using Bcmp.Domain.Assignments;
using Bcmp.Domain.Jobs;

namespace Bcmp.Application.Assignments;

public sealed class AssignmentRuleService(
    IAssignmentRuleRepository assignmentRuleRepository,
    IUserRepository userRepository,
    IPropertyRepository propertyRepository,
    TimeProvider timeProvider) : IAssignmentRuleService
{
    public async Task<IReadOnlyList<AssignmentRuleDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rules = await assignmentRuleRepository.GetAllAsync(cancellationToken);
        return await MapRulesAsync(rules, cancellationToken);
    }

    public async Task<AssignmentRuleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await assignmentRuleRepository.GetByIdAsync(id, cancellationToken);
        if (rule is null)
        {
            return null;
        }

        var mapped = await MapRulesAsync([rule], cancellationToken);
        return mapped.Single();
    }

    public async Task<AssignmentRuleDto> CreateAsync(
        string name,
        Guid targetTrusteeUserId,
        Guid? propertyId,
        JobSource? jobSource,
        IReadOnlyList<string> keywords,
        CancellationToken cancellationToken = default)
    {
        await ValidateTargetTrusteeAsync(targetTrusteeUserId, cancellationToken);
        await ValidatePropertyAsync(propertyId, cancellationToken);

        var existingRules = await assignmentRuleRepository.GetAllAsync(cancellationToken);
        var priority = existingRules.Count == 0 ? 1 : existingRules.Max(rule => rule.Priority) + 1;
        var now = timeProvider.GetUtcNow();
        var rule = AssignmentRule.Create(
            Guid.NewGuid(),
            name,
            priority,
            targetTrusteeUserId,
            propertyId,
            jobSource,
            keywords,
            now);

        await assignmentRuleRepository.AddAsync(rule, cancellationToken);
        return (await MapRulesAsync([rule], cancellationToken)).Single();
    }

    public async Task<AssignmentRuleDto> UpdateAsync(
        Guid id,
        string name,
        bool isEnabled,
        Guid targetTrusteeUserId,
        Guid? propertyId,
        JobSource? jobSource,
        IReadOnlyList<string> keywords,
        CancellationToken cancellationToken = default)
    {
        var rule = await assignmentRuleRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Assignment rule '{id}' was not found.");

        await ValidateTargetTrusteeAsync(targetTrusteeUserId, cancellationToken);
        await ValidatePropertyAsync(propertyId, cancellationToken);

        var updated = rule.WithDetails(
            name,
            isEnabled,
            targetTrusteeUserId,
            propertyId,
            jobSource,
            keywords,
            timeProvider.GetUtcNow());

        await assignmentRuleRepository.UpdateAsync(updated, cancellationToken);
        return (await MapRulesAsync([updated], cancellationToken)).Single();
    }

    public async Task<AssignmentRuleDto> SetEnabledAsync(
        Guid id,
        bool isEnabled,
        CancellationToken cancellationToken = default)
    {
        var rule = await assignmentRuleRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Assignment rule '{id}' was not found.");

        var updated = rule.WithEnabled(isEnabled, timeProvider.GetUtcNow());
        await assignmentRuleRepository.UpdateAsync(updated, cancellationToken);
        return (await MapRulesAsync([updated], cancellationToken)).Single();
    }

    public async Task<IReadOnlyList<AssignmentRuleDto>> ReorderAsync(
        IReadOnlyList<Guid> orderedRuleIds,
        CancellationToken cancellationToken = default)
    {
        var rules = await assignmentRuleRepository.GetAllAsync(cancellationToken);
        if (orderedRuleIds.Count != rules.Count || orderedRuleIds.Distinct().Count() != rules.Count)
        {
            throw new ArgumentException("Rule ordering must include every assignment rule exactly once.", nameof(orderedRuleIds));
        }

        var byId = rules.ToDictionary(rule => rule.Id);
        if (orderedRuleIds.Any(ruleId => !byId.ContainsKey(ruleId)))
        {
            throw new ArgumentException("Rule ordering contains an unknown assignment rule.", nameof(orderedRuleIds));
        }

        var now = timeProvider.GetUtcNow();
        var updated = orderedRuleIds
            .Select((ruleId, index) => byId[ruleId].WithPriority(index + 1, now))
            .ToList();

        await assignmentRuleRepository.UpdateManyAsync(updated, cancellationToken);
        return await MapRulesAsync(updated, cancellationToken);
    }

    private async Task ValidateTargetTrusteeAsync(Guid targetTrusteeUserId, CancellationToken cancellationToken)
    {
        var target = await userRepository.GetByIdAsync(targetTrusteeUserId, cancellationToken)
            ?? throw new KeyNotFoundException($"User '{targetTrusteeUserId}' was not found.");

        if (!target.IsEnabled || target.IsSystem)
        {
            throw new ArgumentException($"User '{targetTrusteeUserId}' is not an enabled trustee.", nameof(targetTrusteeUserId));
        }
    }

    private async Task ValidatePropertyAsync(Guid? propertyId, CancellationToken cancellationToken)
    {
        if (propertyId is null)
        {
            return;
        }

        _ = await propertyRepository.GetByIdAsync(propertyId.Value, cancellationToken)
            ?? throw new KeyNotFoundException($"Property '{propertyId}' was not found.");
    }

    private async Task<IReadOnlyList<AssignmentRuleDto>> MapRulesAsync(
        IReadOnlyList<AssignmentRule> rules,
        CancellationToken cancellationToken)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);
        var userNames = users.ToDictionary(user => user.Id, user => user.DisplayName);
        var properties = await propertyRepository.GetAllAsync(cancellationToken);
        var propertyNames = properties.ToDictionary(property => property.Id, property => property.Name);

        return rules
            .OrderBy(rule => rule.Priority)
            .ThenBy(rule => rule.CreatedAtUtc)
            .Select(rule => AssignmentRuleDto.FromDomain(
                rule,
                userNames.GetValueOrDefault(rule.TargetTrusteeUserId, "Unknown user"),
                rule.PropertyId is Guid propertyId ? propertyNames.GetValueOrDefault(propertyId, "Unknown property") : null))
            .ToList();
    }
}
