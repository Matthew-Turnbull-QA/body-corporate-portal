namespace Bcmp.Application.EmailIntake;

public interface IEmailAcknowledgementSender
{
    Task SendAsync(EmailAcknowledgement acknowledgement, CancellationToken cancellationToken = default);
}
