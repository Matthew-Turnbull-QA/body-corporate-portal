using Bcmp.Api.Authorization;
using Bcmp.Application.EmailIntake;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bcmp.Api.Controllers;

[ApiController]
[Route("api/email-intake")]
[Authorize(Policy = AuthorizationPolicyNames.RequirePortalAdmin)]
public sealed class EmailIntakeController(IEmailIntakeService emailIntakeService) : ControllerBase
{
    [HttpGet("messages")]
    public async Task<IActionResult> GetMessages(CancellationToken cancellationToken)
    {
        var messages = await emailIntakeService.GetRecentMessagesAsync(cancellationToken: cancellationToken);
        return Ok(messages);
    }

    [HttpPost("poll-now")]
    public async Task<IActionResult> PollNow(CancellationToken cancellationToken)
    {
        var result = await emailIntakeService.PollOnceAsync(cancellationToken);
        return Ok(result);
    }
}
