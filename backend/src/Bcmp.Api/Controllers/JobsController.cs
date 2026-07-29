using System.IdentityModel.Tokens.Jwt;
using Bcmp.Api.Authorization;
using Bcmp.Application.Jobs;
using Bcmp.Domain.Jobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bcmp.Api.Controllers;

[ApiController]
[Route("api/jobs")]
[Authorize]
public sealed class JobsController(IJobService jobService) : ControllerBase
{
    public sealed record CreateJobRequest(Guid PropertyId, string Title, string? Description);

    public sealed record UpdateJobStatusRequest(JobStatus Status, string? Note);

    public sealed record AssignTrusteeRequest(Guid? TrusteeUserId);

    public sealed record UpdateStatusHistoryNoteRequest(string? Note);

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicyNames.RequireJobLoad)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var jobs = await jobService.GetAllAsync(cancellationToken);
        return Ok(jobs);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireJobLoad)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var job = await jobService.GetByIdAsync(id, cancellationToken);
        return job is null ? NotFound() : Ok(job);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicyNames.RequireJobCreate)]
    public async Task<IActionResult> Create(CreateJobRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var created = await jobService.CreateJobAsync(request.PropertyId, request.Title, request.Description, JobSource.Manual, userId.Value, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireJobStatusUpdate)]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateJobStatusRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var updated = await jobService.UpdateStatusAsync(id, request.Status, request.Note, userId.Value, cancellationToken);
        return Ok(updated);
    }

    [HttpGet("{id:guid}/status-history")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireJobLoad)]
    public async Task<IActionResult> GetStatusHistory(Guid id, CancellationToken cancellationToken)
    {
        var history = await jobService.GetStatusHistoryAsync(id, cancellationToken);
        return Ok(history);
    }

    [HttpPatch("{id:guid}/status-history/{historyId:guid}/note")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireAdministrator)]
    public async Task<IActionResult> UpdateStatusHistoryNote(
        Guid id,
        Guid historyId,
        UpdateStatusHistoryNoteRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var updated = await jobService.UpdateStatusHistoryNoteAsync(id, historyId, request.Note, userId.Value, cancellationToken);
        return Ok(updated);
    }

    [HttpPatch("{id:guid}/assign")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireJobAssign)]
    public async Task<IActionResult> AssignTrustee(Guid id, AssignTrusteeRequest request, CancellationToken cancellationToken)
    {
        var updated = await jobService.AssignTrusteeAsync(id, request.TrusteeUserId, cancellationToken);
        return Ok(updated);
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return claim is not null && Guid.TryParse(claim, out var userId) ? userId : null;
    }
}
