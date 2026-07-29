namespace Bcmp.Domain.Jobs;

public sealed record JobStatusHistory
{
    public required Guid Id { get; init; }
    public required Guid JobId { get; init; }
    public required JobStatus FromStatus { get; init; }
    public required JobStatus ToStatus { get; init; }
    public string? Note { get; init; }
    public required Guid ChangedByUserId { get; init; }
    public required DateTimeOffset ChangedAtUtc { get; init; }
    public Guid? NoteEditedByUserId { get; init; }
    public DateTimeOffset? NoteEditedAtUtc { get; init; }

    public static JobStatusHistory Create(
        Guid id,
        Guid jobId,
        JobStatus fromStatus,
        JobStatus toStatus,
        string? note,
        Guid changedByUserId,
        DateTimeOffset changedAtUtc)
    {
        if (fromStatus == toStatus)
        {
            throw new ArgumentException("Status history requires a status change.", nameof(toStatus));
        }

        return new JobStatusHistory
        {
            Id = id,
            JobId = jobId,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            Note = NormalizeNote(note),
            ChangedByUserId = changedByUserId,
            ChangedAtUtc = changedAtUtc,
        };
    }

    public JobStatusHistory WithEditedNote(string? note, Guid editedByUserId, DateTimeOffset editedAtUtc) =>
        this with
        {
            Note = NormalizeNote(note),
            NoteEditedByUserId = editedByUserId,
            NoteEditedAtUtc = editedAtUtc,
        };

    private static string? NormalizeNote(string? note)
    {
        var normalized = note?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
