using Bcmp.Application.EmailIntake;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bcmp.Infrastructure.EmailIntake;

public sealed class EmailIntakeHostedService(
    IServiceScopeFactory scopeFactory,
    EmailIntakeOptions options,
    ILogger<EmailIntakeHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enabled)
        {
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(30, options.PollIntervalSeconds));
        using var timer = new PeriodicTimer(interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            await PollOnceAsync(stoppingToken);
            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    private async Task PollOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IEmailIntakeService>();
            var result = await service.PollOnceAsync(cancellationToken);
            logger.LogInformation(
                "Email intake poll fetched {Fetched}, created {Created}, skipped {DuplicatesSkipped}, failed {Failed}.",
                result.Fetched,
                result.Created,
                result.DuplicatesSkipped,
                result.Failed);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Email intake poll failed.");
        }
    }
}
