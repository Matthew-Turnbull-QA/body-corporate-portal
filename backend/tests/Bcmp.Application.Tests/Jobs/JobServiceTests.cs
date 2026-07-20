using Bcmp.Application.Jobs;
using Bcmp.Application.Properties;
using Bcmp.Application.Tests.TestDoubles;
using Bcmp.Domain.Jobs;
using Bcmp.Domain.Properties;
using FluentAssertions;
using NSubstitute;

namespace Bcmp.Application.Tests.Jobs;

[TestFixture]
public class JobServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private IJobRepository _jobRepository = null!;
    private IPropertyRepository _propertyRepository = null!;
    private JobService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _jobRepository = Substitute.For<IJobRepository>();
        _propertyRepository = Substitute.For<IPropertyRepository>();
        _sut = new JobService(_jobRepository, _propertyRepository, new FixedTimeProvider(Now));
    }

    private static Property MakeProperty(Guid? id = null) =>
        Property.Create(id ?? Guid.NewGuid(), "Sunset Villas", "12 Ocean Drive", "North Shore", "NSW", "2000", Now);

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
        var sut = new JobService(_jobRepository, _propertyRepository, new FixedTimeProvider(later));
        _jobRepository.GetByIdAsync(job.Id).Returns(job);
        _propertyRepository.GetByIdAsync(property.Id).Returns(property);

        var result = await sut.UpdateStatusAsync(job.Id, JobStatus.InProgress);

        result.Status.Should().Be(JobStatus.InProgress);
        result.UpdatedAtUtc.Should().Be(later);
        await _jobRepository.Received(1).UpdateAsync(Arg.Is<Job>(j => j.Status == JobStatus.InProgress && j.UpdatedAtUtc == later), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateStatusAsync_UnknownJob_Throws()
    {
        _jobRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Job?)null);

        var act = async () => await _sut.UpdateStatusAsync(Guid.NewGuid(), JobStatus.Completed);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Test]
    public async Task GetAllAsync_MapsDomainJobsToDtosWithPropertyNames()
    {
        var property = MakeProperty();
        var job = Job.Create(Guid.NewGuid(), property.Id, "Leaking roof", "Description", JobSource.Manual, Guid.NewGuid(), Now);
        _jobRepository.GetAllAsync().Returns([job]);
        _propertyRepository.GetAllAsync().Returns([property]);

        var result = await _sut.GetAllAsync();

        result.Should().ContainSingle(j => j.Id == job.Id && j.PropertyName == "Sunset Villas");
    }
}
