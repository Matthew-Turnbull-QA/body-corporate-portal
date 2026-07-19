using Bcmp.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bcmp.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthenticationService authenticationService) : ControllerBase
{
    public sealed record GoogleSignInRequest(string IdToken);

    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<IActionResult> SignInWithGoogle(GoogleSignInRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.SignInWithGoogleAsync(request.IdToken, cancellationToken);

        if (result is null)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Sign-in not permitted.",
                detail: "This Google account is not registered, or has been disabled. Contact your administrator.");
        }

        return Ok(result);
    }
}
