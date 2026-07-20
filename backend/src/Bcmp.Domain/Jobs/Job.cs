namespace Bcmp.Domain.Jobs;

public sealed record Job
{
    public required Guid Id { get; init; }
    public required Guid PropertyId { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required JobStatus Status { get; init; }
    public required JobSource Source { get; init; }
    public required Guid CreatedByUserId { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
    public required DateTimeOffset UpdatedAtUtc { get; init; }

    public static Job Create(
        Guid id,
        Guid propertyId,
        string title,
        string? description,
        JobSource source,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be empty.", nameof(title));
        }

        return new Job
        {
            Id = id,
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
}
