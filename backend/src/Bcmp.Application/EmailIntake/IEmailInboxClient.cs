namespace Bcmp.Application.EmailIntake;

public interface IEmailInboxClient
{
    Task<IReadOnlyList<EmailInboxMessage>> FetchUnreadAsync(
        string folderName,
        int maxMessages,
        CancellationToken cancellationToken = default);

    Task MarkAsSeenAsync(EmailInboxMessage message, CancellationToken cancellationToken = default);
}
