using EmployeeManagement.Application.Abstractions;
using EmployeeManagement.Application.Common;
using EmployeeManagement.Application.Dtos;
using EmployeeManagement.Application.Queries;
using EmployeeManagement.Core.Models;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Api.Tests.Unit;

// Hand-written fakes: the services only depend on interfaces, so unit tests can run them
// against in-memory lists, with no database and no web server.

internal sealed class FakeEmployeeRepository : IEmployeeRepository
{
    public List<Employee> Items { get; } = [];

    public Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(e => e.Id == id));

    public Task<PagedResult<Employee>> SearchAsync(EmployeeQueryParameters query, CancellationToken cancellationToken = default)
    {
        // Same filtering/sorting/paging code that EF Core translates to SQL.
        var filtered = Items.AsQueryable().ApplyFilters(query);
        var page = filtered.ApplySorting(query).ApplyPaging(query).ToList();
        return Task.FromResult(new PagedResult<Employee>(page, query.Page, query.PageSize, filtered.Count()));
    }

    public Task<IReadOnlyList<Employee>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Employee>>(Items.ToList());

    public Task<bool> EmailExistsAsync(string email, int? excludingEmployeeId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(e => e.Email == email && e.Id != excludingEmployeeId));

    public void Add(Employee employee) => Items.Add(employee);
    public void Remove(Employee employee) => Items.Remove(employee);
}

internal sealed class FakeDepartmentRepository(FakeEmployeeRepository employees) : IDepartmentRepository
{
    public List<Department> Items { get; } = [];

    public Task<Department?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(d => d.Id == id));

    public Task<IReadOnlyList<DepartmentResponse>> ListWithHeadcountAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DepartmentResponse>>(Items
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentResponse(d.Id, d.Name, employees.Items.Count(e => e.DepartmentId == d.Id)))
            .ToList());

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(d => string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase)));

    public Task<int> CountEmployeesAsync(int departmentId, CancellationToken cancellationToken = default) =>
        Task.FromResult(employees.Items.Count(e => e.DepartmentId == departmentId));

    public void Add(Department department) => Items.Add(department);
    public void Remove(Department department) => Items.Remove(department);
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(1);
    }
}

internal sealed class FakeCurrentUser(params string[] roles) : ICurrentUser
{
    public bool IsAuthenticated => true;
    public string? UserName => "tester";
    public bool IsInRole(string role) => roles.Contains(role);
}

internal sealed class FakePasswordHasher : IPasswordHasher
{
    public int VerifyCalls { get; private set; }

    public string Hash(AppUser user, string password) => "hash:" + password;

    public bool Verify(AppUser user, string passwordHash, string providedPassword)
    {
        VerifyCalls++;
        return passwordHash == "hash:" + providedPassword;
    }
}

internal sealed class FakeUserRepository : IUserRepository
{
    public List<AppUser> Items { get; } = [];

    public Task<AppUser?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(u => u.UserName == userName));
}

internal sealed class FakeTokenService : ITokenService
{
    public static readonly DateTimeOffset ExpiresAt = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public AccessToken CreateToken(AppUser user) => new($"token-for-{user.UserName}", ExpiresAt);
}

/// <summary>Fixed clock so token expiry times are predictable.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

/// <summary>Captures log entries so tests can assert on what was (and wasn't) logged.</summary>
internal sealed class ListLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message)> Entries { get; } = [];
    public List<string> Scopes { get; } = [];

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        Scopes.Add(state.ToString() ?? string.Empty);
        return NullScope.Instance;
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        Entries.Add((logLevel, formatter(state, exception)));

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}

internal static class Seed
{
    public static (FakeEmployeeRepository Employees, FakeDepartmentRepository Departments) Create()
    {
        var employees = new FakeEmployeeRepository();
        var departments = new FakeDepartmentRepository(employees);
        departments.Items.AddRange([new Department(1, "Engineering"), new Department(2, "Sales"), new Department(3, "Finance")]);
        employees.Items.AddRange(
        [
            new FullTimeEmployee(1, "Alice Smith", 120_000m, new DateOnly(2018, 4, 1), 1, "alice@example.com"),
            new FullTimeEmployee(2, "Bob Jones", 80_000m, new DateOnly(2021, 9, 15), 1, "bob@example.com"),
            new ContractEmployee(3, "Carol White", 60_000m, new DateOnly(2024, 1, 1), 1, new DateOnly(2026, 6, 30), "carol@example.com"),
            new FullTimeEmployee(4, "Dan Brown", 80_000m, new DateOnly(2023, 3, 1), 2),
            new ContractEmployee(5, "Eve Black", 45_000m, new DateOnly(2025, 5, 20), 2, null, "eve@example.com"),
        ]);
        return (employees, departments);
    }
}
