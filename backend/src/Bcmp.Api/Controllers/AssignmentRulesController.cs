using Bcmp.Api.Authorization;
using Bcmp.Application.Assignments;
using Bcmp.Domain.Jobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bcmp.Api.Controllers;

[ApiController]
[Route("api/assignment-rules")]
[Authorize(Policy = AuthorizationPolicyNames.RequirePortalAdmin)]
public sealed class AssignmentRulesController(IAssignmentRuleService assignmentRuleService) : ControllerBase
{
    public sealed record SaveAssignmentRuleRequest(
        string Name,
        Guid TargetTrusteeUserId,
        Guid? PropertyId,
        JobSource? JobSource,
        IReadOnlyList<string> Keywords,
        bool? IsEnabled);

    public sealed record ReorderAssignmentRulesRequest(IReadOnlyList<Guid> RuleIds);

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var rules = await assignmentRuleService.GetAllAsync(cancellationToken);
        return Ok(rules);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var rule = await assignmentRuleService.GetByIdAsync(id, cancellationToken);
        return rule is null ? NotFound() : Ok(rule);
    }

    [HttpPost]
    public async Task<IActionResult> Create(SaveAssignmentRuleRequest request, CancellationToken cancellationToken)
    {
        var created = await assignmentRuleService.CreateAsync(
            request.Name,
            request.TargetTrusteeUserId,
            request.PropertyId,
            request.JobSource,
            request.Keywords,
            cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, SaveAssignmentRuleRequest request, CancellationToken cancellationToken)
    {
        var updated = await assignmentRuleService.UpdateAsync(
            id,
            request.Name,
            request.IsEnabled ?? true,
            request.TargetTrusteeUserId,
            request.PropertyId,
            request.JobSource,
            request.Keywords,
            cancellationToken);
        return Ok(updated);
    }

    [HttpPatch("{id:guid}/enable")]
    public async Task<IActionResult> Enable(Guid id, CancellationToken cancellationToken)
    {
        var updated = await assignmentRuleService.SetEnabledAsync(id, true, cancellationToken);
        return Ok(updated);
    }

    [HttpPatch("{id:guid}/disable")]
    public async Task<IActionResult> Disable(Guid id, CancellationToken cancellationToken)
    {
        var updated = await assignmentRuleService.SetEnabledAsync(id, false, cancellationToken);
        return Ok(updated);
    }

    [HttpPut("order")]
    public async Task<IActionResult> Reorder(ReorderAssignmentRulesRequest request, CancellationToken cancellationToken)
    {
        var rules = await assignmentRuleService.ReorderAsync(request.RuleIds, cancellationToken);
        return Ok(rules);
    }
}
