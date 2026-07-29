using Bcmp.Application.Properties;
using Bcmp.Application.Users;
using Bcmp.Domain.Jobs;
using Bcmp.Domain.Users;

namespace Bcmp.Application.Jobs;

public sealed class JobService(
    IJobRepository jobRepository,
    IPropertyRepository propertyRepository,
    IUserRepository userRepository,
    TimeProvider timeProvider) : IJobService
{
    public async Task<IReadOnlyList<JobDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var jobs = await jobRepository.GetAllAsync(cancellationToken);
        var properties = await propertyRepository.GetAllAsync(cancellationToken);
        var propertyNames = properties.ToDictionary(p => p.Id, p => p.Name);
        var users = await userRepository.GetAllAsync(cancellationToken);
        var userNames = users.ToDictionary(u => u.Id, u => u.DisplayName);

        return jobs
            .Select(job => JobDto.FromDomain(
                job,
                propertyNames.GetValueOrDefault(job.PropertyId, "Unknown property"),
                job.AssignedTrusteeUserId is Guid trusteeId ? userNames.GetValueOrDefault(trusteeId, "Unknown user") : null))
            .ToList();
    }

    public async Task<JobDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var job = await jobRepository.GetByIdAsync(id, cancellationToken);
        if (job is null)
        {
            return null;
        }

        var property = await propertyRepository.GetByIdAsync(job.PropertyId, cancellationToken);
        var trusteeName = await ResolveTrusteeNameAsync(job.AssignedTrusteeUserId, cancellationToken);
        return JobDto.FromDomain(job, property?.Name ?? "Unknown property", trusteeName);
    }

    public async Task<JobDto> CreateJobAsync(
        Guid propertyId,
        string title,
        string? description,
        JobSource source,
        Guid createdByUserId,
        CancellationToken cancellationToken = default)
    {
        var property = await propertyRepository.GetByIdAsync(propertyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Property '{propertyId}' was not found.");

        var job = Job.Create(Guid.NewGuid(), propertyId, title, description, source, createdByUserId, timeProvider.GetUtcNow());
        await jobRepository.AddAsync(job, cancellationToken);
        return JobDto.FromDomain(job, property.Name);
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

        if (job.Status == status)
        {
            throw new InvalidOperationException($"Job '{id}' already has status '{status}'.");
        }

        var now = timeProvider.GetUtcNow();
        var updated = job with { Status = status, UpdatedAtUtc = now };
        var history = JobStatusHistory.Create(Guid.NewGuid(), job.Id, job.Status, status, note, changedByUserId, now);
        await jobRepository.UpdateStatusAsync(updated, history, cancellationToken);

        var property = await propertyRepository.GetByIdAsync(updated.PropertyId, cancellationToken);
        var trusteeName = await ResolveTrusteeNameAsync(updated.AssignedTrusteeUserId, cancellationToken);
        return JobDto.FromDomain(updated, property?.Name ?? "Unknown property", trusteeName);
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
        _ = await jobRepository.GetByIdAsync(jobId, cancellationToken)
            ?? throw new KeyNotFoundException($"Job '{jobId}' was not found.");

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

    public async Task<JobDto> AssignTrusteeAsync(Guid id, Guid? trusteeUserId, CancellationToken cancellationToken = default)
    {
        var job = await jobRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Job '{id}' was not found.");

        string? trusteeName = null;
        if (trusteeUserId is not null)
        {
            var trustee = await userRepository.GetByIdAsync(trusteeUserId.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"User '{trusteeUserId}' was not found.");

            if (trustee.Role != UserRole.Trustee)
            {
                throw new ArgumentException($"User '{trusteeUserId}' is not a Trustee.", nameof(trusteeUserId));
            }

            trusteeName = trustee.DisplayName;
        }

        var updated = job with { AssignedTrusteeUserId = trusteeUserId, UpdatedAtUtc = timeProvider.GetUtcNow() };
        await jobRepository.UpdateAsync(updated, cancellationToken);

        var property = await propertyRepository.GetByIdAsync(updated.PropertyId, cancellationToken);
        return JobDto.FromDomain(updated, property?.Name ?? "Unknown property", trusteeName);
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
}
