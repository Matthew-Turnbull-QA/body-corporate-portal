using Bcmp.Domain.Users;
using Bcmp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bcmp.Infrastructure.Bootstrap;

/// <summary>
/// Seeds the very first Administrator so someone can sign in at all, since there is no
/// self-registration. Idempotent: safe to run on every deploy, only acts the first time.
/// </summary>
public sealed class DbInitializer(
    AppDbContext dbContext,
    IOptions<BootstrapOptions> options,
    ILogger<DbInitializer> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var adminEmail = User.NormalizeEmail(options.Value.AdminEmail);

        var existing = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == adminEmail, cancellationToken);
        if (existing is not null)
        {
            logger.LogInformation("Bootstrap admin {Email} already exists; nothing to seed.", adminEmail);
            return;
        }

        var admin = User.Create(
            Guid.NewGuid(),
            adminEmail,
            string.IsNullOrWhiteSpace(options.Value.AdminDisplayName) ? adminEmail : options.Value.AdminDisplayName,
            UserRole.Administrator,
            DateTimeOffset.UtcNow);

        dbContext.Users.Add(admin);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded bootstrap Administrator {Email}.", adminEmail);
    }
}
