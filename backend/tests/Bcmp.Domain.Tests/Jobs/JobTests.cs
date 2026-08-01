using Bcmp.Domain.Jobs;
using FluentAssertions;

namespace Bcmp.Domain.Tests.Jobs;

[TestFixture]
public class JobTests
{
    private static readonly DateTimeOffset CreatedAtUtc = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Test]
    public void Create_WithValidData_ReturnsOpenJob()
    {
        var propertyId = Guid.NewGuid();
        var createdByUserId = Guid.NewGuid();

        var job = Job.Create(Guid.NewGuid(), " BCMP-000001 ", propertyId, "  Leaking roof  ", "  Water coming through the ceiling  ", JobSource.Manual, createdByUserId, CreatedAtUtc);

        job.JobNumber.Should().Be("BCMP-000001");
        job.PropertyId.Should().Be(propertyId);
        job.Title.Should().Be("Leaking roof");
        job.Description.Should().Be("Water coming through the ceiling");
        job.Status.Should().Be(JobStatus.Open);
        job.Source.Should().Be(JobSource.Manual);
        job.CreatedByUserId.Should().Be(createdByUserId);
        job.CreatedAtUtc.Should().Be(CreatedAtUtc);
        job.UpdatedAtUtc.Should().Be(CreatedAtUtc);
        job.AssignedTrusteeUserId.Should().BeNull();
    }

    [Test]
    public void Create_WithNullDescription_ReturnsEmptyDescription()
    {
        var job = Job.Create(Guid.NewGuid(), "BCMP-000001", Guid.NewGuid(), "Leaking roof", null, JobSource.Manual, Guid.NewGuid(), CreatedAtUtc);

        job.Description.Should().BeEmpty();
    }

    [Test]
    public void Create_WithNullProperty_ReturnsJobWithoutProperty()
    {
        var job = Job.Create(Guid.NewGuid(), "BCMP-000001", null, "Leaking roof", null, JobSource.Email, Guid.NewGuid(), CreatedAtUtc);

        job.PropertyId.Should().BeNull();
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void Create_WithEmptyJobNumber_Throws(string? jobNumber)
    {
        var act = () => Job.Create(Guid.NewGuid(), jobNumber!, Guid.NewGuid(), "Title", "Description", JobSource.Manual, Guid.NewGuid(), CreatedAtUtc);

        act.Should().Throw<ArgumentException>().WithParameterName("jobNumber");
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void Create_WithEmptyTitle_Throws(string? title)
    {
        var act = () => Job.Create(Guid.NewGuid(), "BCMP-000001", Guid.NewGuid(), title!, "Description", JobSource.Manual, Guid.NewGuid(), CreatedAtUtc);

        act.Should().Throw<ArgumentException>().WithParameterName("title");
    }
}
