using Bcmp.Domain.Assignments;

namespace Bcmp.Application.Assignments;

public sealed record AssignmentNotificationDto(
    Guid Id,
    Guid RecipientUserId,
    Guid? JobId,
    string? JobNumber,
    AssignmentNotificationType Type,
    string Subject,
    string Message,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? EmailSentAtUtc,
    string? EmailFailureReason)
{
    public static AssignmentNotificationDto FromDomain(
        AssignmentNotification notification,
        string? jobNumber) => new(
            notification.Id,
            notification.RecipientUserId,
            notification.JobId,
            jobNumber,
            notification.Type,
            notification.Subject,
            notification.Message,
            notification.CreatedAtUtc,
            notification.EmailSentAtUtc,
            notification.EmailFailureReason);
}
