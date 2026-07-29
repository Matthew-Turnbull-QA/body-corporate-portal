using Bcmp.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bcmp.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthenticationService authenticationService) : ControllerBase
{
    public sealed record GoogleSignInRequest(string IdToken);

    public sealed record PasswordSignInRequest(string Email, string Password);

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

    [HttpPost("password")]
    [AllowAnonymous]
    public async Task<IActionResult> SignInWithPassword(PasswordSignInRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.SignInWithPasswordAsync(request.Email, request.Password, cancellationToken);

        if (result is null)
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Sign-in not permitted.",
                detail: "The email or password is incorrect, the user has no local password, or the account has been disabled.");
        }

        return Ok(result);
    }
}
