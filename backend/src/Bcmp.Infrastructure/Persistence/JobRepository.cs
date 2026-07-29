using Bcmp.Application.Jobs;
using Bcmp.Domain.Jobs;
using Microsoft.EntityFrameworkCore;

namespace Bcmp.Infrastructure.Persistence;

public sealed class JobRepository(AppDbContext dbContext) : IJobRepository
{
    public async Task<Job?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Jobs.FirstOrDefaultAsync(j => j.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Job>> GetAllAsync(CancellationToken cancellationToken = default)
        => await dbContext.Jobs.OrderByDescending(j => j.CreatedAtUtc).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<JobStatusHistory>> GetStatusHistoryAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
        => await dbContext.JobStatusHistory
            .Where(history => history.JobId == jobId)
            .OrderByDescending(history => history.ChangedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<JobStatusHistory?> GetStatusHistoryByIdAsync(
        Guid historyId,
        CancellationToken cancellationToken = default)
        => await dbContext.JobStatusHistory.FirstOrDefaultAsync(history => history.Id == historyId, cancellationToken);

    public async Task AddAsync(Job job, CancellationToken cancellationToken = default)
    {
        dbContext.Jobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Job job, CancellationToken cancellationToken = default)
    {
        var trackedEntry = dbContext.ChangeTracker
            .Entries<Job>()
            .FirstOrDefault(entry => entry.Entity.Id == job.Id);

        if (trackedEntry is not null && !ReferenceEquals(trackedEntry.Entity, job))
        {
            dbContext.Entry(trackedEntry.Entity).State = EntityState.Detached;
        }

        dbContext.Jobs.Update(job);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStatusAsync(Job job, JobStatusHistory history, CancellationToken cancellationToken = default)
    {
        TrackUpdatedJob(job);
        dbContext.JobStatusHistory.Add(history);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStatusHistoryAsync(JobStatusHistory history, CancellationToken cancellationToken = default)
    {
        var trackedEntry = dbContext.ChangeTracker
            .Entries<JobStatusHistory>()
            .FirstOrDefault(entry => entry.Entity.Id == history.Id);

        if (trackedEntry is not null && !ReferenceEquals(trackedEntry.Entity, history))
        {
            dbContext.Entry(trackedEntry.Entity).State = EntityState.Detached;
        }

        dbContext.JobStatusHistory.Update(history);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private void TrackUpdatedJob(Job job)
    {
        var trackedEntry = dbContext.ChangeTracker
            .Entries<Job>()
            .FirstOrDefault(entry => entry.Entity.Id == job.Id);

        if (trackedEntry is not null && !ReferenceEquals(trackedEntry.Entity, job))
        {
            dbContext.Entry(trackedEntry.Entity).State = EntityState.Detached;
        }

        dbContext.Jobs.Update(job);
    }
}
