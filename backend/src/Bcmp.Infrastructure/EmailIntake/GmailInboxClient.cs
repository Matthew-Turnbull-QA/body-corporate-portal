using Bcmp.Application.EmailIntake;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Bcmp.Infrastructure.EmailIntake;

public sealed class GmailInboxClient(IOptions<GmailEmailOptions> options) : IEmailInboxClient
{
    private readonly GmailOAuthTokenService _tokenService = new(options);

    public async Task<IReadOnlyList<EmailInboxMessage>> FetchUnreadAsync(
        string folderName,
        int maxMessages,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = await ConnectAsync(cancellationToken);
            var folder = await GetFolderAsync(client, folderName, cancellationToken);
            await folder.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

            var uids = await folder.SearchAsync(SearchQuery.NotSeen, cancellationToken);
            var messages = new List<EmailInboxMessage>();

            foreach (var uid in uids.Take(maxMessages))
            {
                var message = await folder.GetMessageAsync(uid, cancellationToken);
                messages.Add(MapMessage(folder, uid, message));
            }

            await client.DisconnectAsync(true, cancellationToken);
            return messages;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var reason = ex.GetBaseException().Message;
            throw new InvalidOperationException(
                $"Gmail mailbox could not be checked. Confirm the Gmail OAuth secrets, IMAP access, and EmailIntake:FolderName value. Gmail/MailKit reason: {reason}",
                ex);
        }
    }

    public async Task MarkAsSeenAsync(EmailInboxMessage message, CancellationToken cancellationToken = default)
    {
        using var client = await ConnectAsync(cancellationToken);
        var folder = await GetFolderAsync(client, message.FolderName, cancellationToken);
        await folder.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

        await folder.AddFlagsAsync(new UniqueId(message.Uid), MessageFlags.Seen, true, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    private async Task<ImapClient> ConnectAsync(CancellationToken cancellationToken)
    {
        var configured = options.Value;
        ValidateOptions(configured);

        var client = new ImapClient();
        if (configured.AllowInvalidServerCertificate)
        {
            client.ServerCertificateValidationCallback = (_, _, _, _) => true;
        }

        await client.ConnectAsync(configured.ImapHost, configured.ImapPort, SecureSocketOptions.SslOnConnect, cancellationToken);
        var accessToken = await _tokenService.GetAccessTokenAsync(cancellationToken);
        await client.AuthenticateAsync(new SaslMechanismOAuth2(configured.Address, accessToken), cancellationToken);
        return client;
    }

    private static async Task<IMailFolder> GetFolderAsync(
        ImapClient client,
        string folderName,
        CancellationToken cancellationToken)
    {
        if (string.Equals(folderName, "INBOX", StringComparison.OrdinalIgnoreCase))
        {
            return client.Inbox;
        }

        return await client.GetFolderAsync(folderName, cancellationToken);
    }

    private static EmailInboxMessage MapMessage(IMailFolder folder, UniqueId uid, MimeMessage message)
    {
        var sender = message.From.Mailboxes.FirstOrDefault();
        var attachmentFileNames = message.Attachments
            .Select(attachment => attachment.ContentDisposition?.FileName ?? attachment.ContentType.Name)
            .Where(fileName => !string.IsNullOrWhiteSpace(fileName))
            .Cast<string>()
            .ToList();

        return new EmailInboxMessage(
            $"{folder.FullName}:{folder.UidValidity}:{uid.Id}",
            folder.FullName,
            folder.UidValidity,
            uid.Id,
            message.MessageId,
            sender?.Address ?? "unknown-sender@example.invalid",
            sender?.Name,
            message.Subject,
            message.TextBody ?? message.HtmlBody ?? string.Empty,
            message.Date == default ? DateTimeOffset.UtcNow : message.Date.ToUniversalTime(),
            attachmentFileNames);
    }

    private static void ValidateOptions(GmailEmailOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Address)
            || string.IsNullOrWhiteSpace(options.ClientId)
            || string.IsNullOrWhiteSpace(options.ClientSecret)
            || string.IsNullOrWhiteSpace(options.RefreshToken))
        {
            throw new InvalidOperationException("EmailIntake:Gmail:Address, ClientId, ClientSecret, and RefreshToken must be configured.");
        }
    }
}
