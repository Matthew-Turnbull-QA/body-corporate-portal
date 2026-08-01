using Bcmp.Application.Assignments;
using Bcmp.Domain.Assignments;
using Microsoft.EntityFrameworkCore;

namespace Bcmp.Infrastructure.Persistence;

public sealed class AssignmentNotificationRepository(AppDbContext dbContext) : IAssignmentNotificationRepository
{
    public async Task<IReadOnlyList<AssignmentNotification>> GetForRecipientAsync(
        Guid recipientUserId,
        int take,
        CancellationToken cancellationToken = default)
        => await dbContext.AssignmentNotifications
            .Where(notification => notification.RecipientUserId == recipientUserId)
            .OrderByDescending(notification => notification.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(AssignmentNotification notification, CancellationToken cancellationToken = default)
    {
        dbContext.AssignmentNotifications.Add(notification);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AssignmentNotification notification, CancellationToken cancellationToken = default)
    {
        var trackedEntry = dbContext.ChangeTracker
            .Entries<AssignmentNotification>()
            .FirstOrDefault(entry => entry.Entity.Id == notification.Id);

        if (trackedEntry is not null && !ReferenceEquals(trackedEntry.Entity, notification))
        {
            dbContext.Entry(trackedEntry.Entity).State = EntityState.Detached;
        }

        dbContext.AssignmentNotifications.Update(notification);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
