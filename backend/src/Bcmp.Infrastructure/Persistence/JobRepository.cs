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
}
