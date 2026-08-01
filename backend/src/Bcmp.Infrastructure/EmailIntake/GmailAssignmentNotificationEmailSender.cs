using Bcmp.Application.Assignments;
using Bcmp.Domain.Assignments;
using Bcmp.Domain.Users;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Bcmp.Infrastructure.EmailIntake;

public sealed class GmailAssignmentNotificationEmailSender(IOptions<GmailEmailOptions> options)
    : IAssignmentNotificationEmailSender
{
    private readonly GmailOAuthTokenService _tokenService = new(options);

    public async Task SendAsync(
        User recipient,
        AssignmentNotification notification,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var configured = options.Value;
            ValidateOptions(configured);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Rietvlei Body Corporate", configured.Address));
            message.To.Add(MailboxAddress.Parse(recipient.Email));
            message.Subject = notification.Subject;
            message.Body = new TextPart("plain") { Text = notification.Message };

            using var client = new SmtpClient();
            if (configured.AllowInvalidServerCertificate)
            {
                client.ServerCertificateValidationCallback = (_, _, _, _) => true;
            }

            await client.ConnectAsync(configured.SmtpHost, configured.SmtpPort, SecureSocketOptions.StartTls, cancellationToken);
            var accessToken = await _tokenService.GetAccessTokenAsync(cancellationToken);
            await client.AuthenticateAsync(new SaslMechanismOAuth2(configured.Address, accessToken), cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var reason = ex.GetBaseException().Message;
            throw new InvalidOperationException(
                $"Gmail assignment notification could not be sent. Confirm the Gmail OAuth secrets and SMTP access. Gmail/MailKit reason: {reason}",
                ex);
        }
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
