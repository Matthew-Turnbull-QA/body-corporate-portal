using Bcmp.Application.Authorization;
using Bcmp.Application.Assignments;
using Bcmp.Application.Properties;
using Bcmp.Application.Users;
using Bcmp.Domain.Assignments;
using Bcmp.Domain.Jobs;
using Bcmp.Domain.Users;

namespace Bcmp.Application.Jobs;

public sealed class JobService(
    IJobRepository jobRepository,
    IPropertyRepository propertyRepository,
    IUserRepository userRepository,
    IJobNumberGenerator jobNumberGenerator,
    IAssignmentRuleRepository assignmentRuleRepository,
    IAssignmentNotificationRepository assignmentNotificationRepository,
    IAssignmentNotificationEmailSender assignmentNotificationEmailSender,
    TimeProvider timeProvider) : IJobService
{
    public async Task<IReadOnlyList<JobDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var jobs = await jobRepository.GetAllAsync(cancellationToken);
        var properties = await propertyRepository.GetAllAsync(cancellationToken);
        var propertyNames = properties.ToDictionary(p => p.Id, p => p.Name);
        var users = await userRepository.GetAllAsync(cancellationToken);
        var userNames = users.ToDictionary(u => u.Id, u => u.DisplayName);
        var rules = await assignmentRuleRepository.GetAllAsync(cancellationToken);
        var ruleNames = rules.ToDictionary(rule => rule.Id, rule => rule.Name);

        return jobs
            .Select(job => JobDto.FromDomain(
                job,
                job.PropertyId is Guid propertyId ? propertyNames.GetValueOrDefault(propertyId, "Unknown property") : null,
                job.AssignedTrusteeUserId is Guid trusteeId ? userNames.GetValueOrDefault(trusteeId, "Unknown user") : null,
                job.AssignmentRuleId is Guid ruleId ? ruleNames.GetValueOrDefault(ruleId, "Unknown rule") : null))
            .ToList();
    }

    public async Task<JobDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var job = await jobRepository.GetByIdAsync(id, cancellationToken);
        if (job is null)
        {
            return null;
        }

        var property = job.PropertyId is Guid propertyId
            ? await propertyRepository.GetByIdAsync(propertyId, cancellationToken)
            : null;
        var trusteeName = await ResolveTrusteeNameAsync(job.AssignedTrusteeUserId, cancellationToken);
        var ruleName = await ResolveRuleNameAsync(job.AssignmentRuleId, cancellationToken);
        return JobDto.FromDomain(
            job,
            job.PropertyId is null ? null : property?.Name ?? "Unknown property",
            trusteeName,
            ruleName);
    }

    public async Task<JobDto> CreateJobAsync(
        Guid? propertyId,
        string title,
        string? description,
        JobSource source,
        Guid createdByUserId,
        CancellationToken cancellationToken = default)
    {
        if (source == JobSource.Manual && propertyId is null)
        {
            throw new ArgumentException("Manual jobs require a property.", nameof(propertyId));
        }

        var property = propertyId is Guid selectedPropertyId
            ? await propertyRepository.GetByIdAsync(selectedPropertyId, cancellationToken)
                ?? throw new KeyNotFoundException($"Property '{selectedPropertyId}' was not found.")
            : null;

        _ = await GetJobCreatorAsync(createdByUserId, cancellationToken);
        var jobNumber = await jobNumberGenerator.GenerateNextAsync(cancellationToken);
        var job = Job.Create(Guid.NewGuid(), jobNumber, propertyId, title, description, source, createdByUserId, timeProvider.GetUtcNow());
        var assignment = await ResolveAssignmentAsync(job, cancellationToken);
        job = job.WithAssignment(
            assignment.Trustee.Id,
            assignment.Source,
            assignment.RuleId,
            job.CreatedAtUtc);
        await jobRepository.AddAsync(job, cancellationToken);
        await NotifyRoutingWarningsAsync(assignment.SkippedRuleNames, cancellationToken);
        await NotifyAssignedAsync(job, assignment.Trustee, AssignmentNotificationType.Assigned, cancellationToken);
        var ruleName = await ResolveRuleNameAsync(job.AssignmentRuleId, cancellationToken);
        return JobDto.FromDomain(job, property?.Name, assignment.Trustee.DisplayName, ruleName);
    }

    public async Task<JobDto> UpdateJobAsync(
        Guid id,
        Guid? propertyId,
        string title,
        string? description,
        Guid updatedByUserId,
        CancellationToken cancellationToken = default)
    {
        var job = await jobRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Job '{id}' was not found.");

        await EnsureCanMutateJobAsync(job, updatedByUserId, cancellationToken);

        var property = propertyId is Guid selectedPropertyId
            ? await propertyRepository.GetByIdAsync(selectedPropertyId, cancellationToken)
                ?? throw new KeyNotFoundException($"Property '{selectedPropertyId}' was not found.")
            : null;

        var updated = job.WithDetails(propertyId, title, description, timeProvider.GetUtcNow());
        var previousTrusteeId = job.AssignedTrusteeUserId;
        if (ShouldRerouteAfterFirstPropertySelection(job, updated))
        {
            var assignment = await ResolveAssignmentAsync(updated, cancellationToken);
            updated = updated.WithAssignment(
                assignment.Trustee.Id,
                assignment.Source,
                assignment.RuleId,
                updated.UpdatedAtUtc);
            await NotifyRoutingWarningsAsync(assignment.SkippedRuleNames, cancellationToken);
        }

        await jobRepository.UpdateAsync(updated, cancellationToken);

        var trusteeName = await ResolveTrusteeNameAsync(updated.AssignedTrusteeUserId, cancellationToken);
        if (ShouldRerouteAfterFirstPropertySelection(job, updated) && updated.AssignedTrusteeUserId != previousTrusteeId)
        {
            await NotifyReassignmentAsync(updated, previousTrusteeId, updated.AssignedTrusteeUserId, cancellationToken);
        }

        var ruleName = await ResolveRuleNameAsync(updated.AssignmentRuleId, cancellationToken);
        return JobDto.FromDomain(updated, property?.Name, trusteeName, ruleName);
    }

    public async Task<JobDto> UpdateStatusAsync(
        Guid id,
        JobStatus status,
        string? note,
        Guid changedByUserId,
        CancellationToken cancellationToken = default)
    {
        var job = await jobRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Job '{id}' was not found.");

        await EnsureCanMutateJobAsync(job, changedByUserId, cancellationToken);

        if (job.Status == status)
        {
            throw new InvalidOperationException($"Job '{id}' already has status '{status}'.");
        }

        if (job.PropertyId is null && status != JobStatus.Open)
        {
            throw new InvalidOperationException("A property/unit must be selected before changing this job's status.");
        }

        var now = timeProvider.GetUtcNow();
        var updated = job with { Status = status, UpdatedAtUtc = now };
        var history = JobStatusHistory.Create(Guid.NewGuid(), job.Id, job.Status, status, note, changedByUserId, now);
        await jobRepository.UpdateStatusAsync(updated, history, cancellationToken);

        var property = updated.PropertyId is Guid propertyId
            ? await propertyRepository.GetByIdAsync(propertyId, cancellationToken)
            : null;
        var trusteeName = await ResolveTrusteeNameAsync(updated.AssignedTrusteeUserId, cancellationToken);
        var ruleName = await ResolveRuleNameAsync(updated.AssignmentRuleId, cancellationToken);
        return JobDto.FromDomain(
            updated,
            updated.PropertyId is null ? null : property?.Name ?? "Unknown property",
            trusteeName,
            ruleName);
    }

    public async Task<IReadOnlyList<JobStatusHistoryDto>> GetStatusHistoryAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var job = await jobRepository.GetByIdAsync(jobId, cancellationToken)
            ?? throw new KeyNotFoundException($"Job '{jobId}' was not found.");

        var history = await jobRepository.GetStatusHistoryAsync(job.Id, cancellationToken);
        return await MapStatusHistoryAsync(history, cancellationToken);
    }

    public async Task<JobStatusHistoryDto> UpdateStatusHistoryNoteAsync(
        Guid jobId,
        Guid historyId,
        string? note,
        Guid editedByUserId,
        CancellationToken cancellationToken = default)
    {
        var job = await jobRepository.GetByIdAsync(jobId, cancellationToken)
            ?? throw new KeyNotFoundException($"Job '{jobId}' was not found.");

        await EnsureCanMutateJobAsync(job, editedByUserId, cancellationToken);

        var history = await jobRepository.GetStatusHistoryByIdAsync(historyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Status history '{historyId}' was not found.");

        if (history.JobId != jobId)
        {
            throw new KeyNotFoundException($"Status history '{historyId}' was not found for job '{jobId}'.");
        }

        var updated = history.WithEditedNote(note, editedByUserId, timeProvider.GetUtcNow());
        await jobRepository.UpdateStatusHistoryAsync(updated, cancellationToken);

        var mapped = await MapStatusHistoryAsync([updated], cancellationToken);
        return mapped.Single();
    }

    public async Task<JobDto> AssignTrusteeAsync(
        Guid id,
        Guid? trusteeUserId,
        Guid assignedByUserId,
        CancellationToken cancellationToken = default)
    {
        var job = await jobRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Job '{id}' was not found.");

        var assignedBy = await GetEnabledUserAsync(assignedByUserId, cancellationToken);
        if (!assignedBy.IsPortalAdmin)
        {
            throw new ForbiddenAccessException("Only portal admins can assign jobs.");
        }

        string? trusteeName = null;
        if (trusteeUserId is not null)
        {
            var trustee = await userRepository.GetByIdAsync(trusteeUserId.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"User '{trusteeUserId}' was not found.");

            if (!trustee.IsEnabled || trustee.IsSystem)
            {
                throw new ArgumentException($"User '{trusteeUserId}' is not enabled.", nameof(trusteeUserId));
            }

            trusteeName = trustee.DisplayName;
        }

        var previousTrusteeId = job.AssignedTrusteeUserId;
        var updated = job.WithAssignment(
            trusteeUserId,
            trusteeUserId is null ? null : AssignmentSource.ManualOverride,
            null,
            timeProvider.GetUtcNow());
        await jobRepository.UpdateAsync(updated, cancellationToken);
        await NotifyReassignmentAsync(updated, previousTrusteeId, trusteeUserId, cancellationToken);

        var property = updated.PropertyId is Guid propertyId
            ? await propertyRepository.GetByIdAsync(propertyId, cancellationToken)
            : null;
        return JobDto.FromDomain(
            updated,
            updated.PropertyId is null ? null : property?.Name ?? "Unknown property",
            trusteeName);
    }

    private async Task<string?> ResolveTrusteeNameAsync(Guid? trusteeUserId, CancellationToken cancellationToken)
    {
        if (trusteeUserId is null)
        {
            return null;
        }

        var trustee = await userRepository.GetByIdAsync(trusteeUserId.Value, cancellationToken);
        return trustee?.DisplayName ?? "Unknown user";
    }

    private async Task<string?> ResolveRuleNameAsync(Guid? assignmentRuleId, CancellationToken cancellationToken)
    {
        if (assignmentRuleId is null)
        {
            return null;
        }

        var rule = await assignmentRuleRepository.GetByIdAsync(assignmentRuleId.Value, cancellationToken);
        return rule?.Name ?? "Unknown rule";
    }

    private async Task<User> GetJobCreatorAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("The current user was not found.");

        if (!user.IsEnabled && !user.IsSystem)
        {
            throw new UnauthorizedAccessException("The current user is disabled.");
        }

        return user;
    }

    private async Task<User> GetEnabledUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("The current user was not found.");

        if (!user.IsEnabled || user.IsSystem)
        {
            throw new UnauthorizedAccessException("The current user is disabled.");
        }

        return user;
    }

    private async Task EnsureCanMutateJobAsync(Job job, Guid userId, CancellationToken cancellationToken)
    {
        var user = await GetEnabledUserAsync(userId, cancellationToken);
        if (user.IsPortalAdmin || job.AssignedTrusteeUserId == user.Id)
        {
            return;
        }

        throw new ForbiddenAccessException("Only portal admins and the assigned trustee can update this job.");
    }

    private async Task<User> GetNextRoundRobinTrusteeAsync(CancellationToken cancellationToken)
    {
        var eligibleUsers = (await userRepository.GetAllAsync(cancellationToken))
            .Where(user => user.IsEnabled && !user.IsSystem)
            .OrderBy(user => user.CreatedAtUtc)
            .ThenBy(user => user.Id)
            .ToList();

        if (eligibleUsers.Count == 0)
        {
            throw new InvalidOperationException("No enabled trustees are available for job assignment.");
        }

        var eligibleIds = eligibleUsers.Select(user => user.Id).ToHashSet();
        var recentAssignedJob = (await jobRepository.GetAllAsync(cancellationToken))
            .Where(job => job.AssignedTrusteeUserId is Guid trusteeId && eligibleIds.Contains(trusteeId))
            .OrderByDescending(job => job.CreatedAtUtc)
            .ThenByDescending(job => job.Id)
            .FirstOrDefault();

        if (recentAssignedJob?.AssignedTrusteeUserId is not Guid lastTrusteeId)
        {
            return eligibleUsers[0];
        }

        var currentIndex = eligibleUsers.FindIndex(user => user.Id == lastTrusteeId);
        var nextIndex = currentIndex < 0 ? 0 : (currentIndex + 1) % eligibleUsers.Count;
        return eligibleUsers[nextIndex];
    }

    private async Task<AssignmentDecision> ResolveAssignmentAsync(Job job, CancellationToken cancellationToken)
    {
        var skippedRuleNames = new List<string>();
        var rules = (await assignmentRuleRepository.GetAllAsync(cancellationToken))
            .Where(rule => rule.IsEnabled)
            .OrderBy(rule => rule.Priority)
            .ThenBy(rule => rule.CreatedAtUtc)
            .ToList();

        foreach (var rule in rules)
        {
            if (!RuleMatches(rule, job))
            {
                continue;
            }

            var target = await userRepository.GetByIdAsync(rule.TargetTrusteeUserId, cancellationToken);
            if (target is { IsEnabled: true, IsSystem: false })
            {
                return new AssignmentDecision(target, AssignmentSource.Rule, rule.Id, skippedRuleNames);
            }

            skippedRuleNames.Add(rule.Name);
        }

        return new AssignmentDecision(
            await GetNextRoundRobinTrusteeAsync(cancellationToken),
            AssignmentSource.RoundRobinFallback,
            null,
            skippedRuleNames);
    }

    private static bool RuleMatches(AssignmentRule rule, Job job)
    {
        if (rule.PropertyId is Guid propertyId && job.PropertyId != propertyId)
        {
            return false;
        }

        if (rule.JobSource is JobSource source && job.Source != source)
        {
            return false;
        }

        var keywords = rule.GetKeywords();
        if (keywords.Count == 0)
        {
            return true;
        }

        var haystack = $"{job.Title}\n{job.Description}";
        return keywords.Any(keyword => haystack.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ShouldRerouteAfterFirstPropertySelection(Job original, Job updated) =>
        original.Source == JobSource.Email
        && original.PropertyId is null
        && updated.PropertyId is not null
        && original.AssignmentSource != AssignmentSource.ManualOverride;

    private async Task NotifyAssignedAsync(
        Job job,
        User assignedTrustee,
        AssignmentNotificationType type,
        CancellationToken cancellationToken)
    {
        var subject = $"Job #{job.JobNumber} assigned to you";
        var message = $"Job #{job.JobNumber} ({job.Title}) has been assigned to you.";
        await CreateAndSendNotificationAsync(assignedTrustee, job.Id, type, subject, message, cancellationToken);
    }

    private async Task NotifyReassignmentAsync(
        Job job,
        Guid? previousTrusteeId,
        Guid? newTrusteeId,
        CancellationToken cancellationToken)
    {
        if (newTrusteeId is Guid assignedId && assignedId != previousTrusteeId)
        {
            var assigned = await userRepository.GetByIdAsync(assignedId, cancellationToken);
            if (assigned is not null)
            {
                await NotifyAssignedAsync(job, assigned, AssignmentNotificationType.ReassignedTo, cancellationToken);
            }
        }

        if (previousTrusteeId is Guid previousId && previousId != newTrusteeId)
        {
            var previous = await userRepository.GetByIdAsync(previousId, cancellationToken);
            if (previous is not null)
            {
                var subject = $"Job #{job.JobNumber} reassigned";
                var message = $"Job #{job.JobNumber} ({job.Title}) is no longer assigned to you.";
                await CreateAndSendNotificationAsync(previous, job.Id, AssignmentNotificationType.ReassignedAway, subject, message, cancellationToken);
            }
        }
    }

    private async Task NotifyRoutingWarningsAsync(
        IReadOnlyList<string> skippedRuleNames,
        CancellationToken cancellationToken)
    {
        if (skippedRuleNames.Count == 0)
        {
            return;
        }

        var admins = (await userRepository.GetAllAsync(cancellationToken))
            .Where(user => user.IsEnabled && user.IsPortalAdmin && !user.IsSystem)
            .ToList();
        var subject = "Assignment rule target unavailable";
        var message = $"Assignment rules were skipped because their target trustees are unavailable: {string.Join(", ", skippedRuleNames)}.";

        foreach (var admin in admins)
        {
            await CreateAndSendNotificationAsync(admin, null, AssignmentNotificationType.RoutingWarning, subject, message, cancellationToken);
        }
    }

    private async Task CreateAndSendNotificationAsync(
        User recipient,
        Guid? jobId,
        AssignmentNotificationType type,
        string subject,
        string message,
        CancellationToken cancellationToken)
    {
        var notification = AssignmentNotification.Create(
            Guid.NewGuid(),
            recipient.Id,
            jobId,
            type,
            subject,
            message,
            timeProvider.GetUtcNow());

        await assignmentNotificationRepository.AddAsync(notification, cancellationToken);

        try
        {
            await assignmentNotificationEmailSender.SendAsync(recipient, notification, cancellationToken);
            await assignmentNotificationRepository.UpdateAsync(
                notification.WithEmailSent(timeProvider.GetUtcNow()),
                cancellationToken);
        }
        catch (Exception ex)
        {
            await assignmentNotificationRepository.UpdateAsync(
                notification.WithEmailFailure(ex.GetBaseException().Message),
                cancellationToken);
        }
    }

    private async Task<IReadOnlyList<JobStatusHistoryDto>> MapStatusHistoryAsync(
        IReadOnlyList<JobStatusHistory> history,
        CancellationToken cancellationToken)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);
        var userNames = users.ToDictionary(user => user.Id, user => user.DisplayName);

        return history
            .Select(entry => JobStatusHistoryDto.FromDomain(
                entry,
                userNames.GetValueOrDefault(entry.ChangedByUserId, "Unknown user"),
                entry.NoteEditedByUserId is Guid editedByUserId
                    ? userNames.GetValueOrDefault(editedByUserId, "Unknown user")
                    : null))
            .ToList();
    }

    private sealed record AssignmentDecision(
        User Trustee,
        AssignmentSource Source,
        Guid? RuleId,
        IReadOnlyList<string> SkippedRuleNames);
}
