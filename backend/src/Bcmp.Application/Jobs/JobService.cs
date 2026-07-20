using Bcmp.Application.Properties;
using Bcmp.Domain.Jobs;

namespace Bcmp.Application.Jobs;

public sealed class JobService(IJobRepository jobRepository, IPropertyRepository propertyRepository, TimeProvider timeProvider) : IJobService
{
    public async Task<IReadOnlyList<JobDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var jobs = await jobRepository.GetAllAsync(cancellationToken);
        var properties = await propertyRepository.GetAllAsync(cancellationToken);
        var propertyNames = properties.ToDictionary(p => p.Id, p => p.Name);

        return jobs.Select(job => JobDto.FromDomain(job, propertyNames.GetValueOrDefault(job.PropertyId, "Unknown property"))).ToList();
    }

    public async Task<JobDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var job = await jobRepository.GetByIdAsync(id, cancellationToken);
        if (job is null)
        {
            return null;
        }

        var property = await propertyRepository.GetByIdAsync(job.PropertyId, cancellationToken);
        return JobDto.FromDomain(job, property?.Name ?? "Unknown property");
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

    public async Task<JobDto> UpdateStatusAsync(Guid id, JobStatus status, CancellationToken cancellationToken = default)
    {
        var job = await jobRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Job '{id}' was not found.");

        var updated = job with { Status = status, UpdatedAtUtc = timeProvider.GetUtcNow() };
        await jobRepository.UpdateAsync(updated, cancellationToken);

        var property = await propertyRepository.GetByIdAsync(updated.PropertyId, cancellationToken);
        return JobDto.FromDomain(updated, property?.Name ?? "Unknown property");
    }
}
