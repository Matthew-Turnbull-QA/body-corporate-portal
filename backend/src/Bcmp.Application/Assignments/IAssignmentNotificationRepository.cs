using Bcmp.Domain.Assignments;

namespace Bcmp.Application.Assignments;

public interface IAssignmentNotificationRepository
{
    Task<IReadOnlyList<AssignmentNotification>> GetForRecipientAsync(
        Guid recipientUserId,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(AssignmentNotification notification, CancellationToken cancellationToken = default);

    Task UpdateAsync(AssignmentNotification notification, CancellationToken cancellationToken = default);
}
