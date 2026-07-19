using Bcmp.Application.Users;
using Microsoft.Extensions.DependencyInjection;

namespace Bcmp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}
