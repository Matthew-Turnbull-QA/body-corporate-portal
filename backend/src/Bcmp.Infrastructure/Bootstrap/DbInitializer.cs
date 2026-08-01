using Bcmp.Application.EmailIntake;
using Bcmp.Domain.Users;
using Bcmp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bcmp.Infrastructure.Bootstrap;

/// <summary>
/// Seeds the very first portal admin so someone can sign in at all, since there is no
/// self-registration. Idempotent: safe to run on every deploy, only acts the first time.
/// </summary>
public sealed class DbInitializer(
    AppDbContext dbContext,
    IOptions<BootstrapOptions> options,
    EmailIntakeOptions emailIntakeOptions,
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
            DateTimeOffset.UtcNow,
            isPortalAdmin: true);

        dbContext.Users.Add(admin);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded bootstrap portal admin {Email}.", adminEmail);
    }

    public async Task SeedEmailIntakeUserAsync(CancellationToken cancellationToken = default)
    {
        var systemEmail = User.NormalizeEmail(emailIntakeOptions.SystemUserEmail);
        var existing = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == systemEmail, cancellationToken);
        if (existing is not null)
        {
            if (!existing.IsSystem)
            {
                var updated = existing with { IsSystem = true, IsPortalAdmin = false, PasswordHash = null };
                dbContext.Users.Update(updated);
                await dbContext.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Updated existing user {Email} to email-intake system user.", systemEmail);
            }
            else
            {
                logger.LogInformation("Email-intake system user {Email} already exists; nothing to seed.", systemEmail);
            }

            return;
        }

        var systemUser = User.Create(
            Guid.NewGuid(),
            systemEmail,
            "Email Intake",
            DateTimeOffset.UtcNow,
            isPortalAdmin: false,
            isSystem: true);

        dbContext.Users.Add(systemUser);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded email-intake system user {Email}.", systemEmail);
    }
}
