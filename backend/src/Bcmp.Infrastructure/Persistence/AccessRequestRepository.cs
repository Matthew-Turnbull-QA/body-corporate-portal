using Bcmp.Application.AccessRequests;
using Bcmp.Domain.AccessRequests;
using Microsoft.EntityFrameworkCore;

namespace Bcmp.Infrastructure.Persistence;

public sealed class AccessRequestRepository(AppDbContext dbContext) : IAccessRequestRepository
{
    public Task<AccessRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.AccessRequests.SingleOrDefaultAsync(request => request.Id == id, cancellationToken);

    public Task<AccessRequest?> GetPendingByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        dbContext.AccessRequests.SingleOrDefaultAsync(
            request => request.Email == normalizedEmail && request.Status == AccessRequestStatus.Pending,
            cancellationToken);

    public async Task<IReadOnlyList<AccessRequest>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.AccessRequests
            .OrderBy(request => request.Status)
            .ThenByDescending(request => request.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(AccessRequest request, CancellationToken cancellationToken = default)
    {
        dbContext.AccessRequests.Add(request);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AccessRequest request, CancellationToken cancellationToken = default)
    {
        var trackedEntry = dbContext.ChangeTracker.Entries<AccessRequest>().SingleOrDefault(e => e.Entity.Id == request.Id);
        if (trackedEntry is not null)
        {
            trackedEntry.CurrentValues.SetValues(request);
        }
        else
        {
            dbContext.AccessRequests.Update(request);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
