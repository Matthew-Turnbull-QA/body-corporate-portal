using Bcmp.Application.AccessRequests;
using Bcmp.Application.Auth;
using Bcmp.Application.EmailIntake;
using Bcmp.Application.Jobs;
using Bcmp.Application.Properties;
using Bcmp.Application.Users;
using Bcmp.Infrastructure.Auth;
using Bcmp.Infrastructure.Bootstrap;
using Bcmp.Infrastructure.EmailIntake;
using Bcmp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bcmp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing required configuration: ConnectionStrings:Default");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPropertyRepository, PropertyRepository>();
        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IAccessRequestRepository, AccessRequestRepository>();
        services.AddScoped<IEmailIntakeMessageRepository, EmailIntakeMessageRepository>();
        services.AddScoped<IJobNumberGenerator, PostgresJobNumberGenerator>();

        services.AddOptions<GoogleAuthOptions>()
            .Bind(configuration.GetSection(GoogleAuthOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();

        var emailIntakeOptions = configuration
            .GetSection(EmailIntakeOptions.SectionName)
            .Get<EmailIntakeOptions>() ?? new EmailIntakeOptions();
        services.AddSingleton(emailIntakeOptions);
        services.Configure<GmailEmailOptions>(configuration.GetSection(GmailEmailOptions.SectionName));
        services.AddSingleton<GmailOAuthTokenService>();
        services.AddScoped<IEmailInboxClient, GmailInboxClient>();
        services.AddScoped<IEmailAcknowledgementSender, GmailAcknowledgementSender>();
        services.AddHostedService<EmailIntakeHostedService>();

        // Not ValidateOnStart: Bootstrap:AdminEmail is only required when actually seeding (--seed),
        // not on every normal app start.
        services.AddOptions<BootstrapOptions>()
            .Bind(configuration.GetSection(BootstrapOptions.SectionName))
            .ValidateDataAnnotations();
        services.AddScoped<DbInitializer>();

        return services;
    }
}
