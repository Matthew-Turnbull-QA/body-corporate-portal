using Bcmp.Domain.Jobs;

namespace Bcmp.Application.Jobs;

public interface IJobRepository
{
    Task<Job?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Job>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<JobStatusHistory>> GetStatusHistoryAsync(Guid jobId, CancellationToken cancellationToken = default);

    Task<JobStatusHistory?> GetStatusHistoryByIdAsync(Guid historyId, CancellationToken cancellationToken = default);

    Task AddAsync(Job job, CancellationToken cancellationToken = default);

    Task UpdateAsync(Job job, CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(Job job, JobStatusHistory history, CancellationToken cancellationToken = default);

    Task UpdateStatusHistoryAsync(JobStatusHistory history, CancellationToken cancellationToken = default);
}
