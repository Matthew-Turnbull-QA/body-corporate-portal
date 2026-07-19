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
    private UserService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = Substitute.For<IUserRepository>();
        _sut = new UserService(_repository, new FixedTimeProvider(Now));
    }

    private static User AnAdmin(bool enabled = true) => User.Create(
        Guid.NewGuid(), "admin@example.com", "Admin One", UserRole.Administrator, Now) with { IsEnabled = enabled };

    private static User ATrustee(bool enabled = true) => User.Create(
        Guid.NewGuid(), "trustee@example.com", "Trustee One", UserRole.Trustee, Now) with { IsEnabled = enabled };

    [Test]
    public async Task AddUserAsync_WithNewEmail_CreatesAndPersistsUser()
    {
        _repository.GetByEmailAsync("new@example.com").Returns((User?)null);

        var result = await _sut.AddUserAsync("New@Example.com", "New Person", UserRole.Trustee, createdByUserId: null);

        result.Email.Should().Be("new@example.com");
        result.DisplayName.Should().Be("New Person");
        result.IsEnabled.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Is<User>(u => u.Email == "new@example.com"), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AddUserAsync_WithExistingEmail_Throws()
    {
        var existing = ATrustee();
        _repository.GetByEmailAsync(existing.Email).Returns(existing);

        var act = async () => await _sut.AddUserAsync(existing.Email, "Someone Else", UserRole.Trustee, createdByUserId: null);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _repository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateUserAsync_UnknownUser_Throws()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>()).Returns((User?)null);

        var act = async () => await _sut.UpdateUserAsync(Guid.NewGuid(), "New Name", UserRole.Trustee);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Test]
    public async Task UpdateUserAsync_DemotingTheLastEnabledAdministrator_Throws()
    {
        var admin = AnAdmin();
        _repository.GetByIdAsync(admin.Id).Returns(admin);
        _repository.CountEnabledAdministratorsAsync().Returns(1);

        var act = async () => await _sut.UpdateUserAsync(admin.Id, admin.DisplayName, UserRole.Trustee);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateUserAsync_DemotingOneOfSeveralAdministrators_Succeeds()
    {
        var admin = AnAdmin();
        _repository.GetByIdAsync(admin.Id).Returns(admin);
        _repository.CountEnabledAdministratorsAsync().Returns(2);

        var result = await _sut.UpdateUserAsync(admin.Id, admin.DisplayName, UserRole.Trustee);

        result.Role.Should().Be(UserRole.Trustee);
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
}
