using Bcmp.Application.Auth;
using Bcmp.Application.Tests.TestDoubles;
using Bcmp.Application.Users;
using Bcmp.Domain.Users;
using FluentAssertions;
using NSubstitute;

namespace Bcmp.Application.Tests.Users;

[TestFixture]
public class UserServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private IUserRepository _repository = null!;
    private IPasswordHasher _passwordHasher = null!;
    private UserService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = Substitute.For<IUserRepository>();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _sut = new UserService(_repository, _passwordHasher, new FixedTimeProvider(Now));
    }

    private static User AnAdmin(bool enabled = true) => User.Create(
        Guid.NewGuid(), "admin@example.com", "Admin One", UserRole.Administrator, Now) with { IsEnabled = enabled };

    private static User ATrustee(bool enabled = true) => User.Create(
        Guid.NewGuid(), "trustee@example.com", "Trustee One", UserRole.Trustee, Now) with { IsEnabled = enabled };

    [Test]
    public async Task AddUserAsync_WithNewEmail_CreatesAndPersistsUser()
    {
        _repository.GetByEmailAsync("new@example.com").Returns((User?)null);

        var result = await _sut.AddUserAsync("New@Example.com", "New Person", UserRole.Trustee, permissions: null, password: null, createdByUserId: null);

        result.Email.Should().Be("new@example.com");
        result.DisplayName.Should().Be("New Person");
        result.Permissions.Should().BeEquivalentTo([UserPermission.LoadJobs, UserPermission.CreateJobs, UserPermission.UpdateJobStatus]);
        result.IsEnabled.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Is<User>(u => u.Email == "new@example.com"), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AddUserAsync_WithExistingEmail_Throws()
    {
        var existing = ATrustee();
        _repository.GetByEmailAsync(existing.Email).Returns(existing);

        var act = async () => await _sut.AddUserAsync(existing.Email, "Someone Else", UserRole.Trustee, permissions: null, password: null, createdByUserId: null);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _repository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateUserAsync_UnknownUser_Throws()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>()).Returns((User?)null);

        var act = async () => await _sut.UpdateUserAsync(Guid.NewGuid(), "New Name", UserRole.Trustee, permissions: null, newPassword: null);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Test]
    public async Task UpdateUserAsync_DemotingTheLastEnabledAdministrator_Throws()
    {
        var admin = AnAdmin();
        _repository.GetByIdAsync(admin.Id).Returns(admin);
        _repository.CountEnabledAdministratorsAsync().Returns(1);

        var act = async () => await _sut.UpdateUserAsync(admin.Id, admin.DisplayName, UserRole.Trustee, permissions: null, newPassword: null);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateUserAsync_DemotingOneOfSeveralAdministrators_Succeeds()
    {
        var admin = AnAdmin();
        _repository.GetByIdAsync(admin.Id).Returns(admin);
        _repository.CountEnabledAdministratorsAsync().Returns(2);

        var result = await _sut.UpdateUserAsync(admin.Id, admin.DisplayName, UserRole.Trustee, permissions: null, newPassword: null);

        result.Role.Should().Be(UserRole.Trustee);
        result.Permissions.Should().BeEquivalentTo([UserPermission.LoadJobs, UserPermission.CreateJobs, UserPermission.UpdateJobStatus]);
        await _repository.Received(1).UpdateAsync(Arg.Is<User>(u => u.Role == UserRole.Trustee), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DisableUserAsync_LastEnabledAdministrator_Throws()
    {
        var admin = AnAdmin();
        _repository.GetByIdAsync(admin.Id).Returns(admin);
        _repository.CountEnabledAdministratorsAsync().Returns(1);

        var act = async () => await _sut.DisableUserAsync(admin.Id);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DisableUserAsync_NotTheLastAdministrator_Succeeds()
    {
        var admin = AnAdmin();
        _repository.GetByIdAsync(admin.Id).Returns(admin);
        _repository.CountEnabledAdministratorsAsync().Returns(2);

        await _sut.DisableUserAsync(admin.Id);

        await _repository.Received(1).UpdateAsync(Arg.Is<User>(u => !u.IsEnabled), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DisableUserAsync_AlreadyDisabled_IsANoOp()
    {
        var trustee = ATrustee(enabled: false);
        _repository.GetByIdAsync(trustee.Id).Returns(trustee);

        await _sut.DisableUserAsync(trustee.Id);

        await _repository.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DisableUserAsync_DisablingATrustee_NeverChecksAdminCount()
    {
        var trustee = ATrustee();
        _repository.GetByIdAsync(trustee.Id).Returns(trustee);

        await _sut.DisableUserAsync(trustee.Id);

        await _repository.DidNotReceive().CountEnabledAdministratorsAsync(Arg.Any<CancellationToken>());
        await _repository.Received(1).UpdateAsync(Arg.Is<User>(u => !u.IsEnabled), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EnableUserAsync_UnknownUser_Throws()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>()).Returns((User?)null);

        var act = async () => await _sut.EnableUserAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Test]
    public async Task GetAllAsync_MapsDomainUsersToDtos()
    {
        var admin = AnAdmin();
        _repository.GetAllAsync().Returns([admin]);

        var result = await _sut.GetAllAsync();

        result.Should().ContainSingle(u => u.Id == admin.Id && u.Role == UserRole.Administrator);
    }

    [Test]
    public async Task AddUserAsync_WithPassword_HashesPassword()
    {
        _repository.GetByEmailAsync("new@example.com").Returns((User?)null);
        _passwordHasher.HashPassword("password123").Returns("hashed-password");

        var result = await _sut.AddUserAsync("new@example.com", "New Person", UserRole.Trustee, permissions: null, password: "password123", createdByUserId: null);

        result.HasLocalPassword.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Is<User>(u => u.PasswordHash == "hashed-password"), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AddUserAsync_WithShortPassword_Throws()
    {
        _repository.GetByEmailAsync("new@example.com").Returns((User?)null);

        var act = async () => await _sut.AddUserAsync("new@example.com", "New Person", UserRole.Trustee, permissions: null, password: "short", createdByUserId: null);

        await act.Should().ThrowAsync<ArgumentException>();
        _passwordHasher.DidNotReceive().HashPassword(Arg.Any<string>());
        await _repository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateUserAsync_WithExplicitPermissions_PersistsPermissions()
    {
        var trustee = ATrustee();
        _repository.GetByIdAsync(trustee.Id).Returns(trustee);

        var result = await _sut.UpdateUserAsync(
            trustee.Id,
            trustee.DisplayName,
            UserRole.Trustee,
            [UserPermission.LoadJobs, UserPermission.AssignJobs],
            newPassword: null);

        result.Permissions.Should().BeEquivalentTo([UserPermission.LoadJobs, UserPermission.AssignJobs]);
        await _repository.Received(1).UpdateAsync(
            Arg.Is<User>(u => u.Permissions == (UserPermission.LoadJobs | UserPermission.AssignJobs)),
            Arg.Any<CancellationToken>());
    }
}
