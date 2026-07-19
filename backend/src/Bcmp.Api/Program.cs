using System.Text;
using Bcmp.Api.Authorization;
using Bcmp.Api.ErrorHandling;
using Bcmp.Application;
using Bcmp.Domain.Users;
using Bcmp.Infrastructure;
using Bcmp.Infrastructure.Bootstrap;
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
                RoleClaimType = "role",
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(AuthorizationPolicyNames.RequireAdministrator, policy =>
            policy.RequireRole(nameof(UserRole.Administrator)));
        options.AddPolicy(AuthorizationPolicyNames.RequireTrustee, policy =>
            policy.RequireRole(nameof(UserRole.Administrator), nameof(UserRole.Trustee)));
    });

    var app = builder.Build();

    if (args.Contains("--seed"))
    {
        using var seedScope = app.Services.CreateScope();
        var dbInitializer = seedScope.ServiceProvider.GetRequiredService<DbInitializer>();
        await dbInitializer.SeedAsync();
        return;
    }

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseExceptionHandler();

    app.UseHttpsRedirection();

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
