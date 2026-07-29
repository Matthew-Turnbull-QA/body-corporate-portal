using System.IdentityModel.Tokens.Jwt;
using Bcmp.Api.Authorization;
using Bcmp.Application.AccessRequests;
using Bcmp.Domain.AccessRequests;
using Bcmp.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bcmp.Api.Controllers;

[ApiController]
[Route("api/access-requests")]
public sealed class AccessRequestsController(IAccessRequestService accessRequestService) : ControllerBase
{
    public sealed record SubmitAccessRequest(
        string Email,
        string DisplayName,
        string PhoneNumber,
        string PropertyOrUnit,
        AccessRequestRelationship Relationship,
        string? Message);

    public sealed record ApproveAccessRequest(
        UserRole Role,
        IReadOnlyList<UserPermission>? Permissions,
        string? Password,
        string? ReviewNote);

    public sealed record RejectAccessRequest(string? ReviewNote);

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Submit(SubmitAccessRequest request, CancellationToken cancellationToken)
    {
        var submitted = await accessRequestService.SubmitAsync(
            request.Email,
            request.DisplayName,
            request.PhoneNumber,
            request.PropertyOrUnit,
            request.Relationship,
            request.Message,
            cancellationToken);

        return CreatedAtAction(nameof(GetAll), new { id = submitted.Id }, submitted);
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicyNames.RequireAdministrator)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var requests = await accessRequestService.GetAllAsync(cancellationToken);
        return Ok(requests);
    }

    [HttpPatch("{id:guid}/approve")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireAdministrator)]
    public async Task<IActionResult> Approve(Guid id, ApproveAccessRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var approved = await accessRequestService.ApproveAsync(
            id,
            request.Role,
            request.Permissions,
            request.Password,
            userId.Value,
            request.ReviewNote,
            cancellationToken);

        return Ok(approved);
    }

    [HttpPatch("{id:guid}/reject")]
    [Authorize(Policy = AuthorizationPolicyNames.RequireAdministrator)]
    public async Task<IActionResult> Reject(Guid id, RejectAccessRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var rejected = await accessRequestService.RejectAsync(id, userId.Value, request.ReviewNote, cancellationToken);
        return Ok(rejected);
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return claim is not null && Guid.TryParse(claim, out var userId) ? userId : null;
    }
}
