using Bcmp.Application.Auth;
using Bcmp.Application.Jobs;
using Bcmp.Application.Properties;
using Bcmp.Application.Users;
using Microsoft.Extensions.DependencyInjection;

namespace Bcmp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<IJobService, JobService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();

        return services;
    }
}
