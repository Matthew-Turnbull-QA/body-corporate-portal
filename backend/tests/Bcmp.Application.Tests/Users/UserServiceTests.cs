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

    private static User APortalAdmin(bool enabled = true) =>
        User.Create(Guid.NewGuid(), "admin@example.com", "Admin One", Now, isPortalAdmin: true) with { IsEnabled = enabled };

    private static User ATrustee(bool enabled = true) =>
        User.Create(Guid.NewGuid(), "trustee@example.com", "Trustee One", Now) with { IsEnabled = enabled };

    [Test]
    public async Task AddUserAsync_WithNewEmail_CreatesAndPersistsTrustee()
    {
        _repository.GetByEmailAsync("new@example.com").Returns((User?)null);

        var result = await _sut.AddUserAsync("New@Example.com", "New Person", isPortalAdmin: false, password: null, createdByUserId: null);

        result.Email.Should().Be("new@example.com");
        result.DisplayName.Should().Be("New Person");
        result.IsPortalAdmin.Should().BeFalse();
        result.IsEnabled.Should().BeTrue();
        await _repository.Received(1).AddAsync(
            Arg.Is<User>(u => u.Email == "new@example.com" && !u.IsPortalAdmin),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AddUserAsync_WithPortalAdminFlag_PersistsPortalAdmin()
    {
        _repository.GetByEmailAsync("new@example.com").Returns((User?)null);

        var result = await _sut.AddUserAsync("new@example.com", "New Admin", isPortalAdmin: true, password: null, createdByUserId: null);

        result.IsPortalAdmin.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Is<User>(u => u.IsPortalAdmin), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AddUserAsync_WithExistingEmail_Throws()
    {
        var existing = ATrustee();
        _repository.GetByEmailAsync(existing.Email).Returns(existing);

        var act = async () => await _sut.AddUserAsync(existing.Email, "Someone Else", isPortalAdmin: false, password: null, createdByUserId: null);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _repository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateUserAsync_UnknownUser_Throws()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>()).Returns((User?)null);

        var act = async () => await _sut.UpdateUserAsync(Guid.NewGuid(), "New Name", isPortalAdmin: false, newPassword: null);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Test]
    public async Task UpdateUserAsync_RemovingAdminFromLastEnabledPortalAdmin_Throws()
    {
        var admin = APortalAdmin();
        _repository.GetByIdAsync(admin.Id).Returns(admin);
        _repository.CountEnabledPortalAdminsAsync().Returns(1);

        var act = async () => await _sut.UpdateUserAsync(admin.Id, admin.DisplayName, isPortalAdmin: false, newPassword: null);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task UpdateUserAsync_RemovingAdminFromOneOfSeveralPortalAdmins_Succeeds()
    {
        var admin = APortalAdmin();
        _repository.GetByIdAsync(admin.Id).Returns(admin);
        _repository.CountEnabledPortalAdminsAsync().Returns(2);

        var result = await _sut.UpdateUserAsync(admin.Id, admin.DisplayName, isPortalAdmin: false, newPassword: null);

        result.IsPortalAdmin.Should().BeFalse();
        await _repository.Received(1).UpdateAsync(Arg.Is<User>(u => !u.IsPortalAdmin), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DisableUserAsync_LastEnabledPortalAdmin_Throws()
    {
        var admin = APortalAdmin();
        _repository.GetByIdAsync(admin.Id).Returns(admin);
        _repository.CountEnabledPortalAdminsAsync().Returns(1);

        var act = async () => await _sut.DisableUserAsync(admin.Id);

        await act.Should().ThrowAsync<InvalidOperationException>();
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DisableUserAsync_NotTheLastPortalAdmin_Succeeds()
    {
        var admin = APortalAdmin();
        _repository.GetByIdAsync(admin.Id).Returns(admin);
        _repository.CountEnabledPortalAdminsAsync().Returns(2);

        await _sut.DisableUserAsync(admin.Id);

        await _repository.Received(1).UpdateAsync(Arg.Is<User>(u => !u.IsEnabled), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DisableUserAsync_DisablingNormalTrustee_NeverChecksAdminCount()
    {
        var trustee = ATrustee();
        _repository.GetByIdAsync(trustee.Id).Returns(trustee);

        await _sut.DisableUserAsync(trustee.Id);

        await _repository.DidNotReceive().CountEnabledPortalAdminsAsync(Arg.Any<CancellationToken>());
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
        var admin = APortalAdmin();
        _repository.GetAllAsync().Returns([admin]);

        var result = await _sut.GetAllAsync();

        result.Should().ContainSingle(u => u.Id == admin.Id && u.IsPortalAdmin);
    }

    [Test]
    public async Task GetAssignableTrusteesAsync_ReturnsAllEnabledUsers()
    {
        var admin = APortalAdmin();
        var trustee = ATrustee();
        var disabled = ATrustee(enabled: false) with { Email = "disabled@example.com" };
        _repository.GetAllAsync().Returns([admin, trustee, disabled]);

        var result = await _sut.GetAssignableTrusteesAsync();

        result.Select(u => u.Id).Should().BeEquivalentTo([admin.Id, trustee.Id]);
    }

    [Test]
    public async Task AddUserAsync_WithPassword_HashesPassword()
    {
        _repository.GetByEmailAsync("new@example.com").Returns((User?)null);
        _passwordHasher.HashPassword("password123").Returns("hashed-password");

        var result = await _sut.AddUserAsync("new@example.com", "New Person", isPortalAdmin: false, password: "password123", createdByUserId: null);

        result.HasLocalPassword.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Is<User>(u => u.PasswordHash == "hashed-password"), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AddUserAsync_WithShortPassword_Throws()
    {
        _repository.GetByEmailAsync("new@example.com").Returns((User?)null);

        var act = async () => await _sut.AddUserAsync("new@example.com", "New Person", isPortalAdmin: false, password: "short", createdByUserId: null);

        await act.Should().ThrowAsync<ArgumentException>();
        _passwordHasher.DidNotReceive().HashPassword(Arg.Any<string>());
        await _repository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }
}
