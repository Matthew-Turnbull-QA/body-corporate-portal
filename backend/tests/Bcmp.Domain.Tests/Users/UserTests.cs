using Bcmp.Domain.Users;
using FluentAssertions;

namespace Bcmp.Domain.Tests.Users;

[TestFixture]
public class UserTests
{
    private static readonly DateTimeOffset CreatedAtUtc = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Test]
    public void Create_WithValidData_ReturnsEnabledUser()
    {
        var user = User.Create(Guid.NewGuid(), "Trustee@Example.com", "  Jane Trustee  ", UserRole.Trustee, CreatedAtUtc);

        user.Email.Should().Be("trustee@example.com");
        user.DisplayName.Should().Be("Jane Trustee");
        user.Role.Should().Be(UserRole.Trustee);
        user.Permissions.Should().Be(UserPermission.LoadJobs | UserPermission.CreateJobs | UserPermission.UpdateJobStatus);
        user.PasswordHash.Should().BeNull();
        user.IsEnabled.Should().BeTrue();
        user.CreatedAtUtc.Should().Be(CreatedAtUtc);
        user.LastLoginAtUtc.Should().BeNull();
    }

    [Test]
    public void Create_WithAdministratorRole_GetsDefaultAdministratorJobPermissions()
    {
        var user = User.Create(Guid.NewGuid(), "Admin@Example.com", "Admin", UserRole.Administrator, CreatedAtUtc);

        user.Permissions.Should().Be(UserPermission.LoadJobs | UserPermission.CreateJobs | UserPermission.UpdateJobStatus | UserPermission.AssignJobs);
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void Create_WithEmptyEmail_Throws(string? email)
    {
        var act = () => User.Create(Guid.NewGuid(), email!, "Jane Trustee", UserRole.Trustee, CreatedAtUtc);

        act.Should().Throw<ArgumentException>().WithParameterName("email");
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void Create_WithEmptyDisplayName_Throws(string? displayName)
    {
        var act = () => User.Create(Guid.NewGuid(), "trustee@example.com", displayName!, UserRole.Trustee, CreatedAtUtc);

        act.Should().Throw<ArgumentException>().WithParameterName("displayName");
    }

    [Test]
    public void NormalizeEmail_TrimsAndLowercases()
    {
        User.NormalizeEmail("  Trustee@Example.COM  ").Should().Be("trustee@example.com");
    }
}
