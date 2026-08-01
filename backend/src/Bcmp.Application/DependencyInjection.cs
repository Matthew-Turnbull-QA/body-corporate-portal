using Bcmp.Application.AccessRequests;
using Bcmp.Application.Assignments;
using Bcmp.Application.Auth;
using Bcmp.Application.EmailIntake;
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
        services.AddScoped<IAccessRequestService, AccessRequestService>();
        services.AddScoped<IEmailIntakeService, EmailIntakeService>();
        services.AddScoped<IAssignmentRuleService, AssignmentRuleService>();
        services.AddScoped<IAssignmentNotificationService, AssignmentNotificationService>();

        return services;
    }
}
