using Bcmp.Domain.Users;
using FluentAssertions;

namespace Bcmp.Domain.Tests.Users;

[TestFixture]
public class UserTests
{
    private static readonly DateTimeOffset CreatedAtUtc = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Test]
    public void Create_WithValidData_ReturnsEnabledTrustee()
    {
        var user = User.Create(Guid.NewGuid(), "Trustee@Example.com", "  Jane Trustee  ", CreatedAtUtc);

        user.Email.Should().Be("trustee@example.com");
        user.DisplayName.Should().Be("Jane Trustee");
        user.IsPortalAdmin.Should().BeFalse();
        user.PasswordHash.Should().BeNull();
        user.IsEnabled.Should().BeTrue();
        user.CreatedAtUtc.Should().Be(CreatedAtUtc);
        user.LastLoginAtUtc.Should().BeNull();
    }

    [Test]
    public void Create_WithPortalAdminFlag_ReturnsPortalAdminTrustee()
    {
        var user = User.Create(Guid.NewGuid(), "Admin@Example.com", "Admin", CreatedAtUtc, isPortalAdmin: true);

        user.IsPortalAdmin.Should().BeTrue();
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void Create_WithEmptyEmail_Throws(string? email)
    {
        var act = () => User.Create(Guid.NewGuid(), email!, "Jane Trustee", CreatedAtUtc);

        act.Should().Throw<ArgumentException>().WithParameterName("email");
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void Create_WithEmptyDisplayName_Throws(string? displayName)
    {
        var act = () => User.Create(Guid.NewGuid(), "trustee@example.com", displayName!, CreatedAtUtc);

        act.Should().Throw<ArgumentException>().WithParameterName("displayName");
    }

    [Test]
    public void NormalizeEmail_TrimsAndLowercases()
    {
        User.NormalizeEmail("  Trustee@Example.COM  ").Should().Be("trustee@example.com");
    }
}
