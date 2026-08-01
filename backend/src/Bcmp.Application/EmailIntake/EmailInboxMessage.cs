namespace Bcmp.Application.EmailIntake;

public sealed record EmailInboxMessage(
    string ProviderMessageKey,
    string FolderName,
    uint UidValidity,
    uint Uid,
    string? MessageId,
    string SenderEmail,
    string? SenderDisplayName,
    string? Subject,
    string BodyText,
    DateTimeOffset ReceivedAtUtc,
    IReadOnlyList<string> AttachmentFileNames);
