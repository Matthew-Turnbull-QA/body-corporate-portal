using System.Text;
using Bcmp.Api.Authorization;
using Bcmp.Api.ErrorHandling;
using Bcmp.Application;
using Bcmp.Infrastructure;
using Bcmp.Infrastructure.Bootstrap;
using Bcmp.Infrastructure.EmailIntake;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    // Add services to the container.

    builder.Services.AddControllers()
        .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    builder.Services.AddHealthChecks();

    var jwtSection = builder.Configuration.GetSection("Auth:Jwt");
    var jwtSigningKey = jwtSection["SigningKey"]
        ?? throw new InvalidOperationException("Missing required configuration: Auth:Jwt:SigningKey");
    var jwtIssuer = jwtSection["Issuer"] ?? "BodyCorporatePortal";
    var jwtAudience = jwtSection["Audience"] ?? "BodyCorporatePortal";

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            // Keep JWT claim names as-issued (e.g. "sub", not the legacy ClaimTypes.NameIdentifier remap)
            // so token generation and validation agree on claim types without relying on implicit mapping.
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(AuthorizationPolicyNames.RequirePortalAdmin, policy =>
            policy.RequireClaim("portal_admin", "true"));
    });

    const string frontendCorsPolicy = "Frontend";
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(frontendCorsPolicy, policy =>
        {
            if (allowedOrigins.Length > 0)
            {
                // Frontend and backend live on different origins even in production (separate
                // free-tier hosts), so this is required, not just a local-dev convenience.
                policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
            }
        });
    });

    var app = builder.Build();

    if (args.Contains("--seed"))
    {
        using var seedScope = app.Services.CreateScope();
        var dbInitializer = seedScope.ServiceProvider.GetRequiredService<DbInitializer>();
        await dbInitializer.SeedAsync();
        return;
    }

    if (args.Contains("--seed-email-intake"))
    {
        using var seedScope = app.Services.CreateScope();
        var dbInitializer = seedScope.ServiceProvider.GetRequiredService<DbInitializer>();
        await dbInitializer.SeedEmailIntakeUserAsync();
        return;
    }

    if (args.Contains("--gmail-oauth"))
    {
        var gmailOAuthTokenService = app.Services.GetRequiredService<GmailOAuthTokenService>();
        var refreshToken = await gmailOAuthTokenService.GenerateRefreshTokenAsync();

        Console.WriteLine();
        Console.WriteLine("Gmail OAuth refresh token generated.");
        Console.WriteLine("This value is sensitive. Store it in user-secrets, then restart the API.");
        Console.WriteLine();
        Console.WriteLine("From the backend folder, run:");
        Console.WriteLine();
        Console.WriteLine($"""dotnet user-secrets set "EmailIntake:Gmail:RefreshToken" "{refreshToken}" --project src/Bcmp.Api""");
        Console.WriteLine();
        return;
    }

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseExceptionHandler();

    app.UseHttpsRedirection();

    app.UseCors(frontendCorsPolicy);

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/healthz");

    app.Run();
}
finally
{
    Log.CloseAndFlush();
}
