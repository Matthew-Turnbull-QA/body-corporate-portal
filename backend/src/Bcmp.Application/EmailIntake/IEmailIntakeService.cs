namespace Bcmp.Application.EmailIntake;

public interface IEmailIntakeService
{
    Task<EmailIntakePollResult> PollOnceAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmailIntakeMessageDto>> GetRecentMessagesAsync(int take = 50, CancellationToken cancellationToken = default);
}
