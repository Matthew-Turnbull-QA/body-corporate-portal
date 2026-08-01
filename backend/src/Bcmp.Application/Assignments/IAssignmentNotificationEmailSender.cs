using Bcmp.Domain.Assignments;
using Bcmp.Domain.Users;

namespace Bcmp.Application.Assignments;

public interface IAssignmentNotificationEmailSender
{
    Task SendAsync(
        User recipient,
        AssignmentNotification notification,
        CancellationToken cancellationToken = default);
}
