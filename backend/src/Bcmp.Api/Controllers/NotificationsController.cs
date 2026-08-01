using System.IdentityModel.Tokens.Jwt;
using Bcmp.Application.Assignments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bcmp.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController(IAssignmentNotificationService assignmentNotificationService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var notifications = await assignmentNotificationService.GetForUserAsync(userId.Value, 100, cancellationToken);
        return Ok(notifications);
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return claim is not null && Guid.TryParse(claim, out var userId) ? userId : null;
    }
}
