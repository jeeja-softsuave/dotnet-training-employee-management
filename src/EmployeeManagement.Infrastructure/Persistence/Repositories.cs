using EmployeeManagement.Application.Abstractions;
using EmployeeManagement.Application.Common;
using EmployeeManagement.Application.Dtos;
using EmployeeManagement.Application.Queries;
using EmployeeManagement.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Infrastructure.Persistence;

internal sealed class EmployeeRepository(AppDbContext db) : IEmployeeRepository
{
    // Tracked: the caller may change the entity, and SaveChanges will detect it.
    // Include = eager loading: the department is fetched in the same SQL query (a JOIN).
    public Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        db.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<PagedResult<Employee>> SearchAsync(EmployeeQueryParameters query, CancellationToken cancellationToken = default)
    {
        // AsNoTracking: read-only list, so skip the change-tracker bookkeeping (faster, less memory).
        var filtered = db.Employees.AsNoTracking().ApplyFilters(query);

        // Two SQL queries: SELECT COUNT(*) ... and SELECT ... ORDER BY ... LIMIT @take OFFSET @skip.
        var total = await filtered.CountAsync(cancellationToken);
        var items = await filtered
            .Include(e => e.Department)      // JOIN, avoids an N+1 query per row
            .ApplySorting(query)
            .ApplyPaging(query)
            .ToListAsync(cancellationToken);

        return new PagedResult<Employee>(items, query.Page, query.PageSize, total);
    }

    public async Task<IReadOnlyList<Employee>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await db.Employees.AsNoTracking().OrderBy(e => e.Id).ToListAsync(cancellationToken);

    public Task<bool> EmailExistsAsync(string email, int? excludingEmployeeId = null, CancellationToken cancellationToken = default) =>
        db.Employees.AnyAsync(
            e => e.Email == email && (excludingEmployeeId == null || e.Id != excludingEmployeeId),
            cancellationToken);

    public void Add(Employee employee) => db.Employees.Add(employee);

    public void Remove(Employee employee) => db.Employees.Remove(employee);
}

internal sealed class DepartmentRepository(AppDbContext db) : IDepartmentRepository
{
    public Task<Department?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        db.Departments.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    // Projection: only the columns needed, and the headcount is a correlated COUNT in SQL.
    // One query for all departments, instead of one COUNT query per department (N+1).
    public async Task<IReadOnlyList<DepartmentResponse>> ListWithHeadcountAsync(CancellationToken cancellationToken = default) =>
        await db.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentResponse(d.Id, d.Name, db.Employees.Count(e => e.DepartmentId == d.Id)))
            .ToListAsync(cancellationToken);

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default) =>
        db.Departments.AnyAsync(d => d.Name.ToLower() == name.ToLower(), cancellationToken);

    public Task<int> CountEmployeesAsync(int departmentId, CancellationToken cancellationToken = default) =>
        db.Employees.CountAsync(e => e.DepartmentId == departmentId, cancellationToken);

    public void Add(Department department) => db.Departments.Add(department);

    public void Remove(Department department) => db.Departments.Remove(department);
}

internal sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<AppUser?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default) =>
        db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserName == userName, cancellationToken);
}
