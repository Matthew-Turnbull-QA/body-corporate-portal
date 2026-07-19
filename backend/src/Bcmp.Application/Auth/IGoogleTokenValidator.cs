namespace Bcmp.Application.Auth;

public sealed record GoogleIdentity(string Email, string DisplayName);

public interface IGoogleTokenValidator
{
    /// <returns>The verified identity, or null if the token is invalid, expired, or its email is unverified.</returns>
    Task<GoogleIdentity?> ValidateAsync(string idToken, CancellationToken cancellationToken = default);
}
