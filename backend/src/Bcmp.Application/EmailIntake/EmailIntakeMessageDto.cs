using Bcmp.Domain.EmailIntake;

namespace Bcmp.Application.EmailIntake;

public sealed record EmailIntakeMessageDto(
    Guid Id,
    string ProviderMessageKey,
    string? MessageId,
    string SenderEmail,
    string? SenderDisplayName,
    string Subject,
    DateTimeOffset ReceivedAtUtc,
    DateTimeOffset ProcessedAtUtc,
    EmailIntakeMessageStatus Status,
    Guid? JobId,
    string? FailureReason)
{
    public static EmailIntakeMessageDto FromDomain(EmailIntakeMessage message) => new(
        message.Id,
        message.ProviderMessageKey,
        message.MessageId,
        message.SenderEmail,
        message.SenderDisplayName,
        message.Subject,
        message.ReceivedAtUtc,
        message.ProcessedAtUtc,
        message.Status,
        message.JobId,
        message.FailureReason);
}
