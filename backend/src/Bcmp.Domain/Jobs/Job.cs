using Bcmp.Domain.Assignments;

namespace Bcmp.Domain.Jobs;

public sealed record Job
{
    public required Guid Id { get; init; }
    public required string JobNumber { get; init; }
    public required Guid? PropertyId { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required JobStatus Status { get; init; }
    public required JobSource Source { get; init; }
    public required Guid CreatedByUserId { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
    public required DateTimeOffset UpdatedAtUtc { get; init; }
    public Guid? AssignedTrusteeUserId { get; init; }
    public AssignmentSource? AssignmentSource { get; init; }
    public Guid? AssignmentRuleId { get; init; }

    public static Job Create(
        Guid id,
        string jobNumber,
        Guid? propertyId,
        string title,
        string? description,
        JobSource source,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(jobNumber))
        {
            throw new ArgumentException("Job number cannot be empty.", nameof(jobNumber));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be empty.", nameof(title));
        }

        return new Job
        {
            Id = id,
            JobNumber = jobNumber.Trim(),
            PropertyId = propertyId,
            Title = title.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Status = JobStatus.Open,
            Source = source,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = createdAtUtc,
        };
    }

    public Job WithDetails(Guid? propertyId, string title, string? description, DateTimeOffset updatedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be empty.", nameof(title));
        }

        return this with
        {
            PropertyId = propertyId,
            Title = title.Trim(),
            Description = description?.Trim() ?? string.Empty,
            UpdatedAtUtc = updatedAtUtc,
        };
    }

    public Job WithAssignment(
        Guid? assignedTrusteeUserId,
        AssignmentSource? assignmentSource,
        Guid? assignmentRuleId,
        DateTimeOffset updatedAtUtc) =>
        this with
        {
            AssignedTrusteeUserId = assignedTrusteeUserId,
            AssignmentSource = assignmentSource,
            AssignmentRuleId = assignmentRuleId,
            UpdatedAtUtc = updatedAtUtc,
        };
}
