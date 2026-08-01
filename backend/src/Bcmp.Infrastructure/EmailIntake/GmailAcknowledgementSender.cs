using Bcmp.Application.EmailIntake;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Bcmp.Infrastructure.EmailIntake;

public sealed class GmailAcknowledgementSender(IOptions<GmailEmailOptions> options) : IEmailAcknowledgementSender
{
    private readonly GmailOAuthTokenService _tokenService = new(options);

    public async Task SendAsync(EmailAcknowledgement acknowledgement, CancellationToken cancellationToken = default)
    {
        try
        {
            var configured = options.Value;
            ValidateOptions(configured);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Rietvlei Body Corporate", configured.Address));
            message.To.Add(MailboxAddress.Parse(acknowledgement.ToEmail));

            foreach (var bccEmail in acknowledgement.BccEmails.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                message.Bcc.Add(MailboxAddress.Parse(bccEmail));
            }

            message.Subject = acknowledgement.Subject;
            message.Body = new TextPart("plain") { Text = acknowledgement.Body };

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
                $"Gmail acknowledgement could not be sent. Confirm the Gmail OAuth secrets and SMTP access. Gmail/MailKit reason: {reason}",
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
