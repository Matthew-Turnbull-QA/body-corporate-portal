namespace Bcmp.Domain.EmailIntake;

public sealed record EmailIntakeMessage
{
    public required Guid Id { get; init; }
    public required string ProviderMessageKey { get; init; }
    public string? MessageId { get; init; }
    public required string SenderEmail { get; init; }
    public string? SenderDisplayName { get; init; }
    public required string Subject { get; init; }
    public required DateTimeOffset ReceivedAtUtc { get; init; }
    public required DateTimeOffset ProcessedAtUtc { get; init; }
    public required EmailIntakeMessageStatus Status { get; init; }
    public Guid? JobId { get; init; }
    public string? FailureReason { get; init; }

    public static EmailIntakeMessage Create(
        Guid id,
        string providerMessageKey,
        string? messageId,
        string senderEmail,
        string? senderDisplayName,
        string? subject,
        DateTimeOffset receivedAtUtc,
        DateTimeOffset processedAtUtc,
        EmailIntakeMessageStatus status,
        Guid? jobId = null,
        string? failureReason = null)
    {
        if (string.IsNullOrWhiteSpace(providerMessageKey))
        {
            throw new ArgumentException("Provider message key cannot be empty.", nameof(providerMessageKey));
        }

        if (string.IsNullOrWhiteSpace(senderEmail))
        {
            throw new ArgumentException("Sender email cannot be empty.", nameof(senderEmail));
        }

        return new EmailIntakeMessage
        {
            Id = id,
            ProviderMessageKey = providerMessageKey.Trim(),
            MessageId = string.IsNullOrWhiteSpace(messageId) ? null : messageId.Trim(),
            SenderEmail = senderEmail.Trim(),
            SenderDisplayName = string.IsNullOrWhiteSpace(senderDisplayName) ? null : senderDisplayName.Trim(),
            Subject = string.IsNullOrWhiteSpace(subject) ? "(No subject)" : subject.Trim(),
            ReceivedAtUtc = receivedAtUtc,
            ProcessedAtUtc = processedAtUtc,
            Status = status,
            JobId = jobId,
            FailureReason = string.IsNullOrWhiteSpace(failureReason) ? null : failureReason.Trim(),
        };
    }
}
