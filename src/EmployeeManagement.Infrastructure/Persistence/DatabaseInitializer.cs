using EmployeeManagement.Application.Abstractions;
using EmployeeManagement.Core.Models;
using EmployeeManagement.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>Apply pending migrations at startup (Development/Testing only).</summary>
    public bool MigrateOnStartup { get; set; }

    /// <summary>Insert the Week 1 sample departments and employees into an empty database.</summary>
    public bool SeedDemoData { get; set; }

    /// <summary>
    /// Password for the demo users admin / hr / viewer. Not stored in appsettings:
    /// set it with dotnet user-secrets. If empty, no users are created.
    /// </summary>
    public string? DemoUserPassword { get; set; }
}

public sealed class DatabaseInitializer(AppDbContext db, IPasswordHasher passwordHasher, ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(DatabaseOptions options, CancellationToken cancellationToken = default)
    {
        if (options.MigrateOnStartup)
        {
            await db.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Database migrations applied");
        }

        if (options.SeedDemoData)
            await SeedAsync(options.DemoUserPassword, cancellationToken);
    }

    private async Task SeedAsync(string? demoUserPassword, CancellationToken cancellationToken)
    {
        if (!await db.Departments.AnyAsync(cancellationToken))
        {
            // Reuse the Week 1 sample data. The record pointing at department 99 is skipped
            // because that department does not exist (the foreign key would reject it).
            var departments = SampleData.CreateDepartments();
            var departmentIds = departments.Select(d => d.Id).ToHashSet();

            db.Departments.AddRange(departments);
            db.Employees.AddRange(SampleData.CreateEmployees().Where(e => departmentIds.Contains(e.DepartmentId)));
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded {DepartmentCount} departments and sample employees", departments.Count);
        }

        if (string.IsNullOrWhiteSpace(demoUserPassword))
        {
            logger.LogWarning("Database:DemoUserPassword is not set, so no demo users were created. See README.");
            return;
        }

        if (!await db.Users.AnyAsync(cancellationToken))
        {
            foreach (var (userName, role) in new[] { ("admin", Roles.Admin), ("hr", Roles.HR), ("viewer", Roles.Viewer) })
            {
                var user = new AppUser(0, userName, role);
                user.SetPasswordHash(passwordHasher.Hash(user, demoUserPassword));
                db.Users.Add(user);
            }

            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded demo users admin, hr and viewer");
        }
    }
}
