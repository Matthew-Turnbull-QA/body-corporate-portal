using Bcmp.Application.Users;
using Bcmp.Domain.Users;

namespace Bcmp.Application.Auth;

public sealed class AuthenticationService(
    IGoogleTokenValidator googleTokenValidator,
    IUserRepository userRepository,
    IJwtTokenGenerator jwtTokenGenerator,
    TimeProvider timeProvider) : IAuthenticationService
{
    public async Task<AuthenticationResult?> SignInWithGoogleAsync(string googleIdToken, CancellationToken cancellationToken = default)
    {
        var identity = await googleTokenValidator.ValidateAsync(googleIdToken, cancellationToken);
        if (identity is null)
        {
            return null;
        }

        var normalizedEmail = User.NormalizeEmail(identity.Email);
        var user = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null || !user.IsEnabled)
        {
            return null;
        }

        var signedInUser = user with { LastLoginAtUtc = timeProvider.GetUtcNow() };
        await userRepository.UpdateAsync(signedInUser, cancellationToken);

        var accessToken = jwtTokenGenerator.GenerateToken(signedInUser);
        return new AuthenticationResult(accessToken, UserDto.FromDomain(signedInUser));
    }
}
