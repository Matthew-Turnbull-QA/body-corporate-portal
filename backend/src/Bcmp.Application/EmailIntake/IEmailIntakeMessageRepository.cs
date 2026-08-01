using Bcmp.Domain.EmailIntake;

namespace Bcmp.Application.EmailIntake;

public interface IEmailIntakeMessageRepository
{
    Task<bool> ExistsByDedupeKeyAsync(string providerMessageKey, string? messageId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmailIntakeMessage>> GetRecentAsync(int take, CancellationToken cancellationToken = default);

    Task AddAsync(EmailIntakeMessage message, CancellationToken cancellationToken = default);
}
