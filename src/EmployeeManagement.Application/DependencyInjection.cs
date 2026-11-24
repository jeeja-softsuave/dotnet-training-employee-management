using EmployeeManagement.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EmployeeManagement.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Application services are Scoped: one instance per HTTP request, because they use the
    /// scoped repositories / DbContext. A Singleton here would capture a single DbContext for
    /// the whole app lifetime (a "captive dependency").
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
