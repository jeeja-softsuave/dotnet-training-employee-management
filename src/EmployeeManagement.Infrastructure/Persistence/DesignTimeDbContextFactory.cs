using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EmployeeManagement.Infrastructure.Persistence;

/// <summary>
/// Used only by the "dotnet ef" command-line tool (migrations), so it can create a DbContext
/// without starting the whole web app (which needs secrets such as the JWT signing key).
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=employees.db")
            .Options;
        return new AppDbContext(options);
    }
}
