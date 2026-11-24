using EmployeeManagement.Application.Abstractions;
using EmployeeManagement.Infrastructure.Persistence;
using EmployeeManagement.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EmployeeManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        // Scoped (default for AddDbContext): one DbContext per request.
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

        // Scoped: repositories wrap the scoped DbContext, so they must not live longer than it.
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<DatabaseInitializer>();

        // Transient: stateless and cheap, a fresh one each time is fine.
        services.AddTransient<IPasswordHasher, PasswordHasherAdapter>();

        return services;
    }
}
