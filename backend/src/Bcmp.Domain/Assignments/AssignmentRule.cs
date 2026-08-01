using Bcmp.Domain.Jobs;

namespace Bcmp.Domain.Assignments;

public sealed record AssignmentRule
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required bool IsEnabled { get; init; }
    public required int Priority { get; init; }
    public required Guid TargetTrusteeUserId { get; init; }
    public Guid? PropertyId { get; init; }
    public JobSource? JobSource { get; init; }
    public required string Keywords { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
    public required DateTimeOffset UpdatedAtUtc { get; init; }

    public static AssignmentRule Create(
        Guid id,
        string name,
        int priority,
        Guid targetTrusteeUserId,
        Guid? propertyId,
        JobSource? jobSource,
        IEnumerable<string> keywords,
        DateTimeOffset createdAtUtc)
    {
        var normalizedKeywords = NormalizeKeywords(keywords);
        Validate(name, propertyId, jobSource, normalizedKeywords);

        return new AssignmentRule
        {
            Id = id,
            Name = name.Trim(),
            IsEnabled = true,
            Priority = priority,
            TargetTrusteeUserId = targetTrusteeUserId,
            PropertyId = propertyId,
            JobSource = jobSource,
            Keywords = string.Join("\n", normalizedKeywords),
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = createdAtUtc,
        };
    }

    public AssignmentRule WithDetails(
        string name,
        bool isEnabled,
        Guid targetTrusteeUserId,
        Guid? propertyId,
        JobSource? jobSource,
        IEnumerable<string> keywords,
        DateTimeOffset updatedAtUtc)
    {
        var normalizedKeywords = NormalizeKeywords(keywords);
        Validate(name, propertyId, jobSource, normalizedKeywords);

        return this with
        {
            Name = name.Trim(),
            IsEnabled = isEnabled,
            TargetTrusteeUserId = targetTrusteeUserId,
            PropertyId = propertyId,
            JobSource = jobSource,
            Keywords = string.Join("\n", normalizedKeywords),
            UpdatedAtUtc = updatedAtUtc,
        };
    }

    public AssignmentRule WithPriority(int priority, DateTimeOffset updatedAtUtc) =>
        this with { Priority = priority, UpdatedAtUtc = updatedAtUtc };

    public AssignmentRule WithEnabled(bool isEnabled, DateTimeOffset updatedAtUtc) =>
        this with { IsEnabled = isEnabled, UpdatedAtUtc = updatedAtUtc };

    public IReadOnlyList<string> GetKeywords() =>
        Keywords
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

    private static IReadOnlyList<string> NormalizeKeywords(IEnumerable<string> keywords) =>
        keywords
            .Select(keyword => keyword.Trim())
            .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static void Validate(
        string name,
        Guid? propertyId,
        JobSource? jobSource,
        IReadOnlyList<string> keywords)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Assignment rule name cannot be empty.", nameof(name));
        }

        if (propertyId is null && jobSource is null && keywords.Count == 0)
        {
            throw new ArgumentException("Assignment rules require at least one criterion.");
        }
    }
}
