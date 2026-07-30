using Bcmp.Application.Authorization;
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

    private static User MakeTrustee(Guid? id = null, bool enabled = true, DateTimeOffset? createdAt = null) =>
        User.Create(id ?? Guid.NewGuid(), "trustee@example.com", "Terry Trustee", createdAt ?? Now) with { IsEnabled = enabled };

    private static User MakeAdmin(Guid? id = null, bool enabled = true, DateTimeOffset? createdAt = null) =>
        User.Create(id ?? Guid.NewGuid(), "admin@example.com", "Alex Admin", createdAt ?? Now, isPortalAdmin: true) with { IsEnabled = enabled };

    [Test]
    public async Task CreateJobAsync_AssignsFirstEnabledTrusteeWhenNoPriorAssignmentsExist()
    {
        var property = MakeProperty();
        var admin = MakeAdmin(createdAt: Now.AddMinutes(-2));
        var trustee = MakeTrustee(createdAt: Now.AddMinutes(-1));
        _propertyRepository.GetByIdAsync(property.Id).Returns(property);
        _userRepository.GetByIdAsync(admin.Id).Returns(admin);
        _userRepository.GetAllAsync().Returns([trustee, admin]);
        _jobRepository.GetAllAsync().Returns([]);

        var result = await _sut.CreateJobAsync(property.Id, "Leaking roof", "Ceiling in unit 4", JobSource.Manual, admin.Id);

        result.Title.Should().Be("Leaking roof");
        result.Status.Should().Be(JobStatus.Open);
        result.AssignedTrusteeUserId.Should().Be(admin.Id);
        result.AssignedTrusteeName.Should().Be("Alex Admin");
        await _jobRepository.Received(1).AddAsync(
            Arg.Is<Job>(j => j.AssignedTrusteeUserId == admin.Id),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateJobAsync_RotatesToNextEnabledTrusteeAndSkipsDisabledUsers()
    {
        var property = MakeProperty();
        var first = MakeTrustee(Guid.NewGuid(), createdAt: Now.AddMinutes(-3));
        var disabled = MakeTrustee(Guid.NewGuid(), enabled: false, createdAt: Now.AddMinutes(-2)) with { Email = "disabled@example.com" };
        var second = MakeAdmin(Guid.NewGuid(), createdAt: Now.AddMinutes(-1));
        var priorJob = Job.Create(Guid.NewGuid(), property.Id, "Prior", null, JobSource.Manual, first.Id, Now.AddHours(-1))
            with { AssignedTrusteeUserId = first.Id };
        _propertyRepository.GetByIdAsync(property.Id).Returns(property);
        _userRepository.GetByIdAsync(second.Id).Returns(second);
        _userRepository.GetAllAsync().Returns([second, disabled, first]);
        _jobRepository.GetAllAsync().Returns([priorJob]);

        var result = await _sut.CreateJobAsync(property.Id, "New job", null, JobSource.Manual, second.Id);

        result.AssignedTrusteeUserId.Should().Be(second.Id);
        result.AssignedTrusteeName.Should().Be("Alex Admin");
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
    public async Task UpdateJobAsync_AssignedTrusteeCanEditDetails()
    {
        var oldProperty = MakeProperty();
        var newProperty = MakeProperty();
        var trustee = MakeTrustee();
        var job = Job.Create(Guid.NewGuid(), oldProperty.Id, "Old title", "Old", JobSource.Manual, trustee.Id, Now)
            with { AssignedTrusteeUserId = trustee.Id };
        _jobRepository.GetByIdAsync(job.Id).Returns(job);
        _userRepository.GetByIdAsync(trustee.Id).Returns(trustee);
        _propertyRepository.GetByIdAsync(newProperty.Id).Returns(newProperty);

        var result = await _sut.UpdateJobAsync(job.Id, newProperty.Id, "  New title  ", "  New description  ", trustee.Id);

        result.Title.Should().Be("New title");
        result.Description.Should().Be("New description");
        result.PropertyId.Should().Be(newProperty.Id);
        await _jobRepository.Received(1).UpdateAsync(
            Arg.Is<Job>(j => j.Title == "New title" && j.PropertyId == newProperty.Id && j.UpdatedAtUtc == Now),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateJobAsync_OtherTrusteeCannotEditDetails()
    {
        var property = MakeProperty();
        var assigned = MakeTrustee();
        var other = MakeTrustee(Guid.NewGuid()) with { Email = "other@example.com" };
        var job = Job.Create(Guid.NewGuid(), property.Id, "Title", "Description", JobSource.Manual, assigned.Id, Now)
            with { AssignedTrusteeUserId = assigned.Id };
        _jobRepository.GetByIdAsync(job.Id).Returns(job);
        _userRepository.GetByIdAsync(other.Id).Returns(other);

        var act = async () => await _sut.UpdateJobAsync(job.Id, property.Id, "New title", null, other.Id);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        await _jobRepository.DidNotReceive().UpdateAsync(Arg.Any<Job>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateJobAsync_PortalAdminCanEditUnassignedLegacyJob()
    {
        var property = MakeProperty();
        var admin = MakeAdmin();
        var job = Job.Create(Guid.NewGuid(), property.Id, "Title", "Description", JobSource.Manual, admin.Id, Now);
        _jobRepository.GetByIdAsync(job.Id).Returns(job);
        _userRepository.GetByIdAsync(admin.Id).Returns(admin);
        _propertyRepository.GetByIdAsync(property.Id).Returns(property);

        var result = await _sut.UpdateJobAsync(job.Id, property.Id, "New title", null, admin.Id);

        result.Title.Should().Be("New title");
        await _jobRepository.Received(1).UpdateAsync(Arg.Any<Job>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateStatusAsync_AssignedTrusteeUpdatesStatusAndWritesHistory()
    {
        var property = MakeProperty();
        var trustee = MakeTrustee();
        var later = Now.AddDays(1);
        var sut = new JobService(_jobRepository, _propertyRepository, _userRepository, new FixedTimeProvider(later));
        var job = Job.Create(Guid.NewGuid(), property.Id, "Leaking roof", "Description", JobSource.Manual, trustee.Id, Now)
            with { AssignedTrusteeUserId = trustee.Id };
        _jobRepository.GetByIdAsync(job.Id).Returns(job);
        _userRepository.GetByIdAsync(trustee.Id).Returns(trustee);
        _propertyRepository.GetByIdAsync(property.Id).Returns(property);

        var result = await sut.UpdateStatusAsync(job.Id, JobStatus.InProgress, "  Contractor booked  ", trustee.Id);

        result.Status.Should().Be(JobStatus.InProgress);
        result.UpdatedAtUtc.Should().Be(later);
        await _jobRepository.Received(1).UpdateStatusAsync(
            Arg.Is<Job>(j => j.Status == JobStatus.InProgress && j.UpdatedAtUtc == later),
            Arg.Is<JobStatusHistory>(h =>
                h.JobId == job.Id
                && h.FromStatus == JobStatus.Open
                && h.ToStatus == JobStatus.InProgress
                && h.Note == "Contractor booked"
                && h.ChangedByUserId == trustee.Id
                && h.ChangedAtUtc == later),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateStatusAsync_OtherTrusteeCannotUpdateStatus()
    {
        var assigned = MakeTrustee();
        var other = MakeTrustee(Guid.NewGuid()) with { Email = "other@example.com" };
        var job = Job.Create(Guid.NewGuid(), Guid.NewGuid(), "Leaking roof", "Description", JobSource.Manual, assigned.Id, Now)
            with { AssignedTrusteeUserId = assigned.Id };
        _jobRepository.GetByIdAsync(job.Id).Returns(job);
        _userRepository.GetByIdAsync(other.Id).Returns(other);

        var act = async () => await _sut.UpdateStatusAsync(job.Id, JobStatus.Completed, null, other.Id);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        await _jobRepository.DidNotReceive().UpdateStatusAsync(
            Arg.Any<Job>(),
            Arg.Any<JobStatusHistory>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateStatusAsync_WithSameStatus_ThrowsAndDoesNotCreateHistory()
    {
        var trustee = MakeTrustee();
        var job = Job.Create(Guid.NewGuid(), Guid.NewGuid(), "Leaking roof", "Description", JobSource.Manual, trustee.Id, Now)
            with { AssignedTrusteeUserId = trustee.Id };
        _jobRepository.GetByIdAsync(job.Id).Returns(job);
        _userRepository.GetByIdAsync(trustee.Id).Returns(trustee);

        var act = async () => await _sut.UpdateStatusAsync(job.Id, JobStatus.Open, "No change", trustee.Id);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _jobRepository.DidNotReceive().UpdateStatusAsync(
            Arg.Any<Job>(),
            Arg.Any<JobStatusHistory>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetAllAsync_MapsDomainJobsToDtosWithPropertyAndTrusteeNames()
    {
        var property = MakeProperty();
        var trustee = MakeTrustee();
        var job = Job.Create(Guid.NewGuid(), property.Id, "Leaking roof", "Description", JobSource.Manual, trustee.Id, Now)
            with { AssignedTrusteeUserId = trustee.Id };
        _jobRepository.GetAllAsync().Returns([job]);
        _propertyRepository.GetAllAsync().Returns([property]);
        _userRepository.GetAllAsync().Returns([trustee]);

        var result = await _sut.GetAllAsync();

        result.Should().ContainSingle(j =>
            j.Id == job.Id
            && j.PropertyName == "Sunset Villas"
            && j.AssignedTrusteeName == "Terry Trustee");
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
    public async Task UpdateStatusHistoryNoteAsync_AssignedTrusteeCanUpdateNote()
    {
        var editor = MakeTrustee();
        var job = Job.Create(Guid.NewGuid(), Guid.NewGuid(), "Leaking roof", "Description", JobSource.Manual, Guid.NewGuid(), Now)
            with { AssignedTrusteeUserId = editor.Id };
        var history = JobStatusHistory.Create(Guid.NewGuid(), job.Id, JobStatus.Open, JobStatus.Completed, "Original", Guid.NewGuid(), Now);
        var later = Now.AddDays(1);
        var sut = new JobService(_jobRepository, _propertyRepository, _userRepository, new FixedTimeProvider(later));
        _jobRepository.GetByIdAsync(job.Id).Returns(job);
        _jobRepository.GetStatusHistoryByIdAsync(history.Id).Returns(history);
        _userRepository.GetByIdAsync(editor.Id).Returns(editor);
        _userRepository.GetAllAsync().Returns([editor]);

        var result = await sut.UpdateStatusHistoryNoteAsync(job.Id, history.Id, "  Corrected  ", editor.Id);

        result.Note.Should().Be("Corrected");
        result.NoteEditedByUserId.Should().Be(editor.Id);
        result.NoteEditedAtUtc.Should().Be(later);
        await _jobRepository.Received(1).UpdateStatusHistoryAsync(
            Arg.Is<JobStatusHistory>(entry => entry.Id == history.Id && entry.Note == "Corrected"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AssignTrusteeAsync_PortalAdminCanAssignEnabledTrustee()
    {
        var property = MakeProperty();
        var admin = MakeAdmin();
        var trustee = MakeTrustee();
        var job = Job.Create(Guid.NewGuid(), property.Id, "Leaking roof", "Description", JobSource.Manual, admin.Id, Now);
        _jobRepository.GetByIdAsync(job.Id).Returns(job);
        _propertyRepository.GetByIdAsync(property.Id).Returns(property);
        _userRepository.GetByIdAsync(admin.Id).Returns(admin);
        _userRepository.GetByIdAsync(trustee.Id).Returns(trustee);

        var result = await _sut.AssignTrusteeAsync(job.Id, trustee.Id, admin.Id);

        result.AssignedTrusteeUserId.Should().Be(trustee.Id);
        result.AssignedTrusteeName.Should().Be("Terry Trustee");
        await _jobRepository.Received(1).UpdateAsync(Arg.Is<Job>(j => j.AssignedTrusteeUserId == trustee.Id), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AssignTrusteeAsync_NonAdminTrusteeCannotAssign()
    {
        var trustee = MakeTrustee();
        var job = Job.Create(Guid.NewGuid(), Guid.NewGuid(), "Leaking roof", "Description", JobSource.Manual, trustee.Id, Now);
        _jobRepository.GetByIdAsync(job.Id).Returns(job);
        _userRepository.GetByIdAsync(trustee.Id).Returns(trustee);

        var act = async () => await _sut.AssignTrusteeAsync(job.Id, trustee.Id, trustee.Id);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        await _jobRepository.DidNotReceive().UpdateAsync(Arg.Any<Job>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AssignTrusteeAsync_WithDisabledTargetUser_Throws()
    {
        var admin = MakeAdmin();
        var disabled = MakeTrustee(enabled: false);
        var job = Job.Create(Guid.NewGuid(), Guid.NewGuid(), "Leaking roof", "Description", JobSource.Manual, admin.Id, Now);
        _jobRepository.GetByIdAsync(job.Id).Returns(job);
        _userRepository.GetByIdAsync(admin.Id).Returns(admin);
        _userRepository.GetByIdAsync(disabled.Id).Returns(disabled);

        var act = async () => await _sut.AssignTrusteeAsync(job.Id, disabled.Id, admin.Id);

        await act.Should().ThrowAsync<ArgumentException>();
        await _jobRepository.DidNotReceive().UpdateAsync(Arg.Any<Job>(), Arg.Any<CancellationToken>());
    }
}
