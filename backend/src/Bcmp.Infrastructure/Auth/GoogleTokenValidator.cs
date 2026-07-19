using Bcmp.Application.Auth;
using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bcmp.Infrastructure.Auth;

public sealed class GoogleTokenValidator(
    IOptions<GoogleAuthOptions> options,
    ILogger<GoogleTokenValidator> logger) : IGoogleTokenValidator
{
    public async Task<GoogleIdentity?> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [options.Value.ClientId],
            });

            if (!payload.EmailVerified)
            {
                logger.LogWarning("Rejected a Google sign-in for {Email}: email not verified by Google.", payload.Email);
                return null;
            }

            return new GoogleIdentity(payload.Email, string.IsNullOrWhiteSpace(payload.Name) ? payload.Email : payload.Name);
        }
        catch (InvalidJwtException ex)
        {
            logger.LogWarning(ex, "Rejected an invalid Google ID token.");
            return null;
        }
    }
}
