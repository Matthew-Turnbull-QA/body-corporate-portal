using Bcmp.Application.Jobs;
using Bcmp.Application.Properties;
using Bcmp.Application.Tests.TestDoubles;
using Bcmp.Application.Users;
using Bcmp.Domain.Jobs;
using Bcmp.Domain.Properties;
using Bcmp.Domain.Users;
using FluentAssertions;
using NSubstitute;

namespace Bcmp.Application.Tests.Jobs;

[TestFixture]
public class JobServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private IJobRepository _jobRepository = null!;
    private IPropertyRepository _propertyRepository = null!;
    private IUserRepository _userRepository = null!;
    private JobService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _jobRepository = Substitute.For<IJobRepository>();
        _propertyRepository = Substitute.For<IPropertyRepository>();
        _userRepository = Substitute.For<IUserRepository>();
        _sut = new JobService(_jobRepository, _propertyRepository, _userRepository, new FixedTimeProvider(Now));
    }

    private static Property MakeProperty(Guid? id = null) =>
        Property.Create(id ?? Guid.NewGuid(), "Sunset Villas", "12 Ocean Drive", "North Shore", "NSW", "2000", Now);

    private static User MakeTrustee(Guid? id = null) =>
        User.Create(id ?? Guid.NewGuid(), "trustee@example.com", "Terry Trustee", UserRole.Trustee, Now);

    private static User MakeAdmin(Guid? id = null) =>
        User.Create(id ?? Guid.NewGuid(), "admin@example.com", "Alex Admin", UserRole.Administrator, Now);

    [Test]
    public async Task CreateJobAsync_WithKnownProperty_CreatesOpenJob()
    {
        var property = MakeProperty();
        var createdByUserId = Guid.NewGuid();
        _propertyRepository.GetByIdAsync(property.Id).Returns(property);

        var result = await _sut.CreateJobAsync(property.Id, "Leaking roof", "Ceiling in unit 4", JobSource.Manual, createdByUserId);

        result.Title.Should().Be("Leaking roof");
        result.Status.Should().Be(JobStatus.Open);
        result.PropertyName.Should().Be("Sunset Villas");
        result.AssignedTrusteeUserId.Should().BeNull();
        await _jobRepository.Received(1).AddAsync(Arg.Is<Job>(j => j.Title == "Leaking roof" && j.PropertyId == property.Id), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateJobAsync_WithUnknownProperty_Throws()
    {
        _propertyRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Property?)null);

        var act = async () => await _sut.CreateJobAsync(Guid.NewGuid(), "Leaking roof", "Description", JobSource.Manual, Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
        await _jobRepository.DidNotReceive().AddAsync(Arg.Any<Job>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateStatusAsync_WithKnownJob_UpdatesStatusAndUpdatedAtUtc()
    {
        var property = MakeProperty();
        var job = Job.Create(Guid.NewGuid(), property.Id, "Leaking roof", "Description", JobSource.Manual, Guid.NewGuid(), Now);
        var later = Now.AddDays(1);
        var sut = new JobService(_jobRepository, _propertyRepository, _userRepository, new FixedTimeProvider(later));
        _jobRepository.GetByIdAsync(job.Id).Returns(job);
        _propertyRepository.GetByIdAsync(property.Id).Returns(property);
        var changedByUserId = Guid.NewGuid();

        var result = await sut.UpdateStatusAsync(job.Id, JobStatus.InProgress, "  Contractor booked  ", changedByUserId);

        result.Status.Should().Be(JobStatus.InProgress);
        result.UpdatedAtUtc.Should().Be(later);
        await _jobRepository.Received(1).UpdateStatusAsync(
            Arg.Is<Job>(j => j.Status == JobStatus.InProgress && j.UpdatedAtUtc == later),
            Arg.Is<JobStatusHistory>(h =>
                h.JobId == job.Id
                && h.FromStatus == JobStatus.Open
                && h.ToStatus == JobStatus.InProgress
                && h.Note == "Contractor booked"
                && h.ChangedByUserId == changedByUserId
                && h.ChangedAtUtc == later),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateStatusAsync_UnknownJob_Throws()
    {
        _jobRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Job?)null);

        var act = async () => await _sut.UpdateStatusAsync(Guid.NewGuid(), JobStatus.Completed, null, Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Test]
    public async Task UpdateStatusAsync_WithSameStatus_ThrowsAndDoesNotCreateHistory()
    {
        var job = Job.Create(Guid.NewGuid(), Guid.NewGuid(), "Leaking roof", "Description", JobSource.Manual, Guid.NewGuid(), Now);
        _jobRepository.GetByIdAsync(job.Id).Returns(job);

        var act = async () => await _sut.UpdateStatusAsync(job.Id, JobStatus.Open, "No change", Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _jobRepository.DidNotReceive().UpdateStatusAsync(
            Arg.Any<Job>(),
            Arg.Any<JobStatusHistory>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetAllAsync_MapsDomainJobsToDtosWithPropertyNames()
    {
        var property = MakeProperty();
        var job = Job.Create(Guid.NewGuid(), property.Id, "Leaking roof", "Description", JobSource.Manual, Guid.NewGuid(), Now);
        _jobRepository.GetAllAsync().Returns([job]);
        _propertyRepository.GetAllAsync().Returns([property]);
        _userRepository.GetAllAsync().Returns([]);

        var result = await _sut.GetAllAsync();

        result.Should().ContainSingle(j => j.Id == job.Id && j.PropertyName == "Sunset Villas");
    }

    [Test]
    public async Task GetStatusHistoryAsync_WithKnownJob_ReturnsHistoryWithUserNames()
    {
        var changedBy = MakeTrustee();
        var editedBy = MakeAdmin();
        var job = Job.Create(Guid.NewGuid(), Guid.NewGuid(), "Leaking roof", "Description", JobSource.Manual, changedBy.Id, Now);
        var history = JobStatusHistory.Create(
            Guid.NewGuid(),
            job.Id,
            JobStatus.Open,
            JobStatus.InProgress,
            "Started",
            changedBy.Id,
            Now) with
            {
                NoteEditedByUserId = editedBy.Id,
                NoteEditedAtUtc = Now.AddHours(1),
            };
        _jobRepository.GetByIdAsync(job.Id).Returns(job);
        _jobRepository.GetStatusHistoryAsync(job.Id).Returns([history]);
        _userRepository.GetAllAsync().Returns([changedBy, editedBy]);

        var result = await _sut.GetStatusHistoryAsync(job.Id);

        result.Should().ContainSingle(entry =>
            entry.Id == history.Id
            && entry.ChangedByDisplayName == "Terry Trustee"
            && entry.NoteEditedByDisplayName == "Alex Admin");
    }

    [Test]
    public async Task GetStatusHistoryAsync_UnknownJob_Throws()
    {
        _jobRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Job?)null);

        var act = async () => await _sut.GetStatusHistoryAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Test]
    public async Task UpdateStatusHistoryNoteAsync_WithKnownHistory_UpdatesNoteOnly()
    {
        var editor = MakeAdmin();
        var job = Job.Create(Guid.NewGuid(), Guid.NewGuid(), "Leaking roof", "Description", JobSource.Manual, Guid.NewGuid(), Now);
        var history = JobStatusHistory.Create(
            Guid.NewGuid(),
            job.Id,
            JobStatus.Open,
            JobStatus.Completed,
            "Original",
            Guid.NewGuid(),
            Now);
        var later = Now.AddDays(1);
        var sut = new JobService(_jobRepository, _propertyRepository, _userRepository, new FixedTimeProvider(later));
        _jobRepository.GetByIdAsync(job.Id).Returns(job);
        _jobRepository.GetStatusHistoryByIdAsync(history.Id).Returns(history);
        _userRepository.GetAllAsync().Returns([editor]);

        var result = await sut.UpdateStatusHistoryNoteAsync(job.Id, history.Id, "  Corrected  ", editor.Id);

        result.Note.Should().Be("Corrected");
        result.NoteEditedByUserId.Should().Be(editor.Id);
        result.NoteEditedAtUtc.Should().Be(later);
        await _jobRepository.Received(1).UpdateStatusHistoryAsync(
            Arg.Is<JobStatusHistory>(entry =>
                entry.Id == history.Id
                && entry.Note == "Corrected"
                && entry.FromStatus == history.FromStatus
                && entry.ToStatus == history.ToStatus
                && entry.ChangedByUserId == history.ChangedByUserId
                && entry.ChangedAtUtc == history.ChangedAtUtc
                && entry.NoteEditedByUserId == editor.Id
                && entry.NoteEditedAtUtc == later),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateStatusHistoryNoteAsync_WithHistoryForDifferentJob_Throws()
    {
        var job = Job.Create(Guid.NewGuid(), Guid.NewGuid(), "Leaking roof", "Description", JobSource.Manual, Guid.NewGuid(), Now);
        var history = JobStatusHistory.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            JobStatus.Open,
            JobStatus.Completed,
            null,
            Guid.NewGuid(),
            Now);
        _jobRepository.GetByIdAsync(job.Id).Returns(job);
        _jobRepository.GetStatusHistoryByIdAsync(history.Id).Returns(history);

        var act = async () => await _sut.UpdateStatusHistoryNoteAsync(job.Id, history.Id, "Corrected", Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
        await _jobRepository.DidNotReceive().UpdateStatusHistoryAsync(
            Arg.Any<JobStatusHistory>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AssignTrusteeAsync_WithKnownTrustee_AssignsAndReturnsName()
    {
        var property = MakeProperty();
        var trustee = MakeTrustee();
        var job = Job.Create(Guid.NewGuid(), property.Id, "Leaking roof", "Description", JobSource.Manual, Guid.NewGuid(), Now);
        _jobRepository.GetByIdAsync(job.Id).Returns(job);
        _propertyRepository.GetByIdAsync(property.Id).Returns(property);
        _userRepository.GetByIdAsync(trustee.Id).Returns(trustee);

        var result = await _sut.AssignTrusteeAsync(job.Id, trustee.Id);

        result.AssignedTrusteeUserId.Should().Be(trustee.Id);
        result.AssignedTrusteeName.Should().Be("Terry Trustee");
        await _jobRepository.Received(1).UpdateAsync(Arg.Is<Job>(j => j.AssignedTrusteeUserId == trustee.Id), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AssignTrusteeAsync_WithNull_ClearsAssignment()
    {
        var property = MakeProperty();
        var trustee = MakeTrustee();
        var job = Job.Create(Guid.NewGuid(), property.Id, "Leaking roof", "Description", JobSource.Manual, Guid.NewGuid(), Now) with { AssignedTrusteeUserId = trustee.Id };
        _jobRepository.GetByIdAsync(job.Id).Returns(job);
        _propertyRepository.GetByIdAsync(property.Id).Returns(property);

        var result = await _sut.AssignTrusteeAsync(job.Id, null);

        result.AssignedTrusteeUserId.Should().BeNull();
        result.AssignedTrusteeName.Should().BeNull();
        await _jobRepository.Received(1).UpdateAsync(Arg.Is<Job>(j => j.AssignedTrusteeUserId == null), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AssignTrusteeAsync_WithNonTrusteeUser_Throws()
    {
        var property = MakeProperty();
        var admin = MakeAdmin();
        var job = Job.Create(Guid.NewGuid(), property.Id, "Leaking roof", "Description", JobSource.Manual, Guid.NewGuid(), Now);
        _jobRepository.GetByIdAsync(job.Id).Returns(job);
        _userRepository.GetByIdAsync(admin.Id).Returns(admin);

        var act = async () => await _sut.AssignTrusteeAsync(job.Id, admin.Id);

        await act.Should().ThrowAsync<ArgumentException>();
        await _jobRepository.DidNotReceive().UpdateAsync(Arg.Any<Job>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AssignTrusteeAsync_WithUnknownUser_Throws()
    {
        var property = MakeProperty();
        var job = Job.Create(Guid.NewGuid(), property.Id, "Leaking roof", "Description", JobSource.Manual, Guid.NewGuid(), Now);
        _jobRepository.GetByIdAsync(job.Id).Returns(job);
        _userRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((User?)null);

        var act = async () => await _sut.AssignTrusteeAsync(job.Id, Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Test]
    public async Task AssignTrusteeAsync_UnknownJob_Throws()
    {
        _jobRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Job?)null);

        var act = async () => await _sut.AssignTrusteeAsync(Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
