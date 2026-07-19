using Bcmp.Application.Auth;
using Bcmp.Application.Tests.TestDoubles;
using Bcmp.Application.Users;
using Bcmp.Domain.Users;
using FluentAssertions;
using NSubstitute;

namespace Bcmp.Application.Tests.Auth;

[TestFixture]
public class AuthenticationServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private const string GoogleIdToken = "a-google-id-token";

    private IGoogleTokenValidator _googleTokenValidator = null!;
    private IUserRepository _userRepository = null!;
    private IJwtTokenGenerator _jwtTokenGenerator = null!;
    private AuthenticationService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _googleTokenValidator = Substitute.For<IGoogleTokenValidator>();
        _userRepository = Substitute.For<IUserRepository>();
        _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        _sut = new AuthenticationService(_googleTokenValidator, _userRepository, _jwtTokenGenerator, new FixedTimeProvider(Now));
    }

    [Test]
    public async Task SignInWithGoogleAsync_InvalidGoogleToken_ReturnsNullWithoutTouchingUsers()
    {
        _googleTokenValidator.ValidateAsync(GoogleIdToken).Returns((GoogleIdentity?)null);

        var result = await _sut.SignInWithGoogleAsync(GoogleIdToken);

        result.Should().BeNull();
        await _userRepository.DidNotReceive().GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        _jwtTokenGenerator.DidNotReceive().GenerateToken(Arg.Any<User>());
    }

    [Test]
    public async Task SignInWithGoogleAsync_EmailNotProvisioned_ReturnsNull()
    {
        _googleTokenValidator.ValidateAsync(GoogleIdToken).Returns(new GoogleIdentity("unknown@example.com", "Unknown Person"));
        _userRepository.GetByEmailAsync("unknown@example.com").Returns((User?)null);

        var result = await _sut.SignInWithGoogleAsync(GoogleIdToken);

        result.Should().BeNull();
        _jwtTokenGenerator.DidNotReceive().GenerateToken(Arg.Any<User>());
    }

    [Test]
    public async Task SignInWithGoogleAsync_DisabledUser_ReturnsNull()
    {
        var disabledUser = User.Create(Guid.NewGuid(), "trustee@example.com", "Trustee One", UserRole.Trustee, Now) with { IsEnabled = false };
        _googleTokenValidator.ValidateAsync(GoogleIdToken).Returns(new GoogleIdentity(disabledUser.Email, disabledUser.DisplayName));
        _userRepository.GetByEmailAsync(disabledUser.Email).Returns(disabledUser);

        var result = await _sut.SignInWithGoogleAsync(GoogleIdToken);

        result.Should().BeNull();
        _jwtTokenGenerator.DidNotReceive().GenerateToken(Arg.Any<User>());
        await _userRepository.DidNotReceive().UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SignInWithGoogleAsync_EnabledUser_IssuesTokenAndRecordsLastLogin()
    {
        var user = User.Create(Guid.NewGuid(), "trustee@example.com", "Trustee One", UserRole.Trustee, Now);
        _googleTokenValidator.ValidateAsync(GoogleIdToken).Returns(new GoogleIdentity(user.Email, user.DisplayName));
        _userRepository.GetByEmailAsync(user.Email).Returns(user);
        _jwtTokenGenerator.GenerateToken(Arg.Any<User>()).Returns("signed-jwt");

        var result = await _sut.SignInWithGoogleAsync(GoogleIdToken);

        result.Should().NotBeNull();
        result!.AccessToken.Should().Be("signed-jwt");
        result.User.Email.Should().Be(user.Email);
        await _userRepository.Received(1).UpdateAsync(Arg.Is<User>(u => u.LastLoginAtUtc == Now), Arg.Any<CancellationToken>());
        _jwtTokenGenerator.Received(1).GenerateToken(Arg.Is<User>(u => u.LastLoginAtUtc == Now));
    }
}
