using Bcmp.Domain.Jobs;

namespace Bcmp.Application.Jobs;

public sealed record JobStatusHistoryDto(
    Guid Id,
    Guid JobId,
    JobStatus FromStatus,
    JobStatus ToStatus,
    string? Note,
    Guid ChangedByUserId,
    string ChangedByDisplayName,
    DateTimeOffset ChangedAtUtc,
    Guid? NoteEditedByUserId,
    string? NoteEditedByDisplayName,
    DateTimeOffset? NoteEditedAtUtc)
{
    public static JobStatusHistoryDto FromDomain(
        JobStatusHistory history,
        string changedByDisplayName,
        string? noteEditedByDisplayName = null) => new(
            history.Id,
            history.JobId,
            history.FromStatus,
            history.ToStatus,
            history.Note,
            history.ChangedByUserId,
            changedByDisplayName,
            history.ChangedAtUtc,
            history.NoteEditedByUserId,
            noteEditedByDisplayName,
            history.NoteEditedAtUtc);
}
