using System.IdentityModel.Tokens.Jwt;
using Bcmp.Api.Authorization;
using Bcmp.Application.Users;
using Bcmp.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bcmp.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    public sealed record CreateUserRequest(string Email, string DisplayName, UserRole Role);

    public sealed record UpdateUserRequest(string DisplayName, UserRole Role);

    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var user = await userService.GetByIdAsync(userId.Value, cancellationToken);
        return user is null ? Unauthorized() : Ok(user);
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicyNames.RequireAdministrator)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var users = await userService.GetAllAsync(cancellationToken);
        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireAdministrator)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var user = await userService.GetByIdAsync(id, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicyNames.RequireAdministrator)]
    public async Task<IActionResult> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var createdUser = await userService.AddUserAsync(
            request.Email,
            request.DisplayName,
            request.Role,
            GetCurrentUserId(),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = createdUser.Id }, createdUser);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireAdministrator)]
    public async Task<IActionResult> Update(Guid id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var updatedUser = await userService.UpdateUserAsync(id, request.DisplayName, request.Role, cancellationToken);
        return Ok(updatedUser);
    }

    [HttpPatch("{id:guid}/enable")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireAdministrator)]
    public async Task<IActionResult> Enable(Guid id, CancellationToken cancellationToken)
    {
        await userService.EnableUserAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/disable")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireAdministrator)]
    public async Task<IActionResult> Disable(Guid id, CancellationToken cancellationToken)
    {
        await userService.DisableUserAsync(id, cancellationToken);
        return NoContent();
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return claim is not null && Guid.TryParse(claim, out var userId) ? userId : null;
    }
}
