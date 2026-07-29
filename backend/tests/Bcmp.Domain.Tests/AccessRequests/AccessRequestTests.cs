using Bcmp.Domain.AccessRequests;
using FluentAssertions;

namespace Bcmp.Domain.Tests.AccessRequests;

[TestFixture]
public class AccessRequestTests
{
    private static readonly DateTimeOffset CreatedAtUtc = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Test]
    public void Create_WithValidData_ReturnsPendingRequest()
    {
        var request = AccessRequest.Create(
            Guid.NewGuid(),
            "  Person@Example.com  ",
            "  Pat Person  ",
            "  0123456789  ",
            "  Unit 4  ",
            AccessRequestRelationship.Owner,
            "  Need access  ",
            CreatedAtUtc);

        request.Email.Should().Be("person@example.com");
        request.DisplayName.Should().Be("Pat Person");
        request.PhoneNumber.Should().Be("0123456789");
        request.PropertyOrUnit.Should().Be("Unit 4");
        request.Message.Should().Be("Need access");
        request.Status.Should().Be(AccessRequestStatus.Pending);
        request.CreatedAtUtc.Should().Be(CreatedAtUtc);
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void Create_WithEmptyPhoneNumber_Throws(string? phoneNumber)
    {
        var act = () => AccessRequest.Create(
            Guid.NewGuid(),
            "person@example.com",
            "Pat Person",
            phoneNumber!,
            "Unit 4",
            AccessRequestRelationship.Owner,
            null,
            CreatedAtUtc);

        act.Should().Throw<ArgumentException>().WithParameterName("phoneNumber");
    }
}
