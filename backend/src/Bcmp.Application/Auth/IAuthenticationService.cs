using Bcmp.Application.Users;

namespace Bcmp.Application.Auth;

public sealed record AuthenticationResult(string AccessToken, UserDto User);

public interface IAuthenticationService
{
    /// <summary>
    /// Validates the Google ID token, then requires the verified email to already exist and be
    /// enabled in the Users table. No user is ever created here — there is no self-registration.
    /// </summary>
    /// <returns>An access token, or null if the Google token is invalid, or the email is unknown or disabled.</returns>
    Task<AuthenticationResult?> SignInWithGoogleAsync(string googleIdToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Signs in with a locally stored password hash. The email must already exist, be enabled,
    /// and have a local password set.
    /// </summary>
    /// <returns>An access token, or null if the credentials are invalid or the user is disabled.</returns>
    Task<AuthenticationResult?> SignInWithPasswordAsync(string email, string password, CancellationToken cancellationToken = default);
}
