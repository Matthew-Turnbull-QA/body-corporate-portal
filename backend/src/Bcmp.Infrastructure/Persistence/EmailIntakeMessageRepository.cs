using Bcmp.Application.EmailIntake;
using Bcmp.Domain.EmailIntake;
using Microsoft.EntityFrameworkCore;

namespace Bcmp.Infrastructure.Persistence;

public sealed class EmailIntakeMessageRepository(AppDbContext dbContext) : IEmailIntakeMessageRepository
{
    public Task<bool> ExistsByDedupeKeyAsync(
        string providerMessageKey,
        string? messageId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.EmailIntakeMessages.AnyAsync(
            message => message.ProviderMessageKey == providerMessageKey
                || (messageId != null && message.MessageId == messageId),
            cancellationToken);
    }

    public async Task<IReadOnlyList<EmailIntakeMessage>> GetRecentAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.EmailIntakeMessages
            .OrderByDescending(message => message.ProcessedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(EmailIntakeMessage message, CancellationToken cancellationToken = default)
    {
        dbContext.EmailIntakeMessages.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
