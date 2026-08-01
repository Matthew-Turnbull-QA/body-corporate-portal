namespace Bcmp.Domain.Assignments;

public sealed record AssignmentNotification
{
    public required Guid Id { get; init; }
    public required Guid RecipientUserId { get; init; }
    public Guid? JobId { get; init; }
    public required AssignmentNotificationType Type { get; init; }
    public required string Subject { get; init; }
    public required string Message { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset? EmailSentAtUtc { get; init; }
    public string? EmailFailureReason { get; init; }

    public static AssignmentNotification Create(
        Guid id,
        Guid recipientUserId,
        Guid? jobId,
        AssignmentNotificationType type,
        string subject,
        string message,
        DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new ArgumentException("Notification subject cannot be empty.", nameof(subject));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Notification message cannot be empty.", nameof(message));
        }

        return new AssignmentNotification
        {
            Id = id,
            RecipientUserId = recipientUserId,
            JobId = jobId,
            Type = type,
            Subject = subject.Trim(),
            Message = message.Trim(),
            CreatedAtUtc = createdAtUtc,
        };
    }

    public AssignmentNotification WithEmailSent(DateTimeOffset sentAtUtc) =>
        this with { EmailSentAtUtc = sentAtUtc, EmailFailureReason = null };

    public AssignmentNotification WithEmailFailure(string failureReason) =>
        this with { EmailFailureReason = failureReason.Trim() };
}
