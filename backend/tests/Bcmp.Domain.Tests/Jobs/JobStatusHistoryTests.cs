using Bcmp.Domain.Jobs;
using FluentAssertions;

namespace Bcmp.Domain.Tests.Jobs;

[TestFixture]
public class JobStatusHistoryTests
{
    private static readonly DateTimeOffset ChangedAtUtc = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Test]
    public void Create_WithStatusChange_ReturnsTrimmedOptionalNote()
    {
        var jobId = Guid.NewGuid();
        var changedByUserId = Guid.NewGuid();

        var history = JobStatusHistory.Create(
            Guid.NewGuid(),
            jobId,
            JobStatus.Open,
            JobStatus.InProgress,
            "  Called contractor  ",
            changedByUserId,
            ChangedAtUtc);

        history.JobId.Should().Be(jobId);
        history.FromStatus.Should().Be(JobStatus.Open);
        history.ToStatus.Should().Be(JobStatus.InProgress);
        history.Note.Should().Be("Called contractor");
        history.ChangedByUserId.Should().Be(changedByUserId);
        history.ChangedAtUtc.Should().Be(ChangedAtUtc);
        history.NoteEditedByUserId.Should().BeNull();
        history.NoteEditedAtUtc.Should().BeNull();
    }

    [Test]
    public void Create_WithBlankNote_StoresNullNote()
    {
        var history = JobStatusHistory.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            JobStatus.Open,
            JobStatus.Completed,
            "   ",
            Guid.NewGuid(),
            ChangedAtUtc);

        history.Note.Should().BeNull();
    }

    [Test]
    public void Create_WithSameStatus_Throws()
    {
        var act = () => JobStatusHistory.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            JobStatus.Open,
            JobStatus.Open,
            null,
            Guid.NewGuid(),
            ChangedAtUtc);

        act.Should().Throw<ArgumentException>().WithParameterName("toStatus");
    }

    [Test]
    public void WithEditedNote_UpdatesOnlyNoteEditFields()
    {
        var history = JobStatusHistory.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            JobStatus.Open,
            JobStatus.Cancelled,
            "Original",
            Guid.NewGuid(),
            ChangedAtUtc);
        var editorId = Guid.NewGuid();
        var editedAtUtc = ChangedAtUtc.AddHours(1);

        var updated = history.WithEditedNote("  Corrected note  ", editorId, editedAtUtc);

        updated.Note.Should().Be("Corrected note");
        updated.NoteEditedByUserId.Should().Be(editorId);
        updated.NoteEditedAtUtc.Should().Be(editedAtUtc);
        updated.FromStatus.Should().Be(history.FromStatus);
        updated.ToStatus.Should().Be(history.ToStatus);
        updated.ChangedByUserId.Should().Be(history.ChangedByUserId);
        updated.ChangedAtUtc.Should().Be(history.ChangedAtUtc);
    }
}
