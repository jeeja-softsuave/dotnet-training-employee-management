using EmployeeManagement.Application.Dtos;
using EmployeeManagement.Core.Models;

namespace EmployeeManagement.Application.Queries;

/// <summary>
/// Filtering, sorting and paging rules written against IQueryable&lt;Employee&gt;.
///
/// - With EF Core, the expressions are translated to SQL (WHERE / ORDER BY / LIMIT OFFSET),
///   so only one page of rows ever leaves the database.
/// - In unit tests, the same methods run on a List via AsQueryable(), so the rules can be
///   tested without a database.
///
/// Only operations both LINQ-to-Objects and EF Core can translate are used here
/// (e.g. ToLower().Contains instead of a culture-aware comparison).
/// </summary>
public static class EmployeeQueryExtensions
{
    public static IQueryable<Employee> ApplyFilters(this IQueryable<Employee> employees, EmployeeQueryParameters query)
    {
        ArgumentNullException.ThrowIfNull(employees);
        ArgumentNullException.ThrowIfNull(query);

        if (query.DepartmentId is { } departmentId)
            employees = employees.Where(e => e.DepartmentId == departmentId);

        employees = query.Type switch
        {
            EmploymentType.FullTime => employees.Where(e => e is FullTimeEmployee),
            EmploymentType.Contract => employees.Where(e => e is ContractEmployee),
            _ => employees,
        };

        // Both bounds inclusive (same rule as Week 1's SalaryRange, see DEF-001).
        if (query.MinSalary is { } min)
            employees = employees.Where(e => e.Salary >= min);
        if (query.MaxSalary is { } max)
            employees = employees.Where(e => e.Salary <= max);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            employees = employees.Where(e =>
                e.Name.ToLower().Contains(term) ||
                (e.Email != null && e.Email.ToLower().Contains(term)));
        }

        return employees;
    }

    /// <summary>
    /// Sorts by the requested field, then by Id. The Id tie-break makes the order deterministic,
    /// which paging needs: without it, two rows with the same salary could swap between pages.
    /// </summary>
    public static IQueryable<Employee> ApplySorting(this IQueryable<Employee> employees, EmployeeQueryParameters query)
    {
        ArgumentNullException.ThrowIfNull(employees);
        ArgumentNullException.ThrowIfNull(query);

        var desc = query.IsDescending;
        IOrderedQueryable<Employee> ordered = query.SortBy.ToLowerInvariant() switch
        {
            "name" => desc ? employees.OrderByDescending(e => e.Name) : employees.OrderBy(e => e.Name),
            "salary" => desc ? employees.OrderByDescending(e => e.Salary) : employees.OrderBy(e => e.Salary),
            "hiredate" => desc ? employees.OrderByDescending(e => e.HireDate) : employees.OrderBy(e => e.HireDate),
            _ => desc ? employees.OrderByDescending(e => e.Id) : employees.OrderBy(e => e.Id),
        };

        return desc ? ordered.ThenByDescending(e => e.Id) : ordered.ThenBy(e => e.Id);
    }

    public static IQueryable<Employee> ApplyPaging(this IQueryable<Employee> employees, EmployeeQueryParameters query)
    {
        ArgumentNullException.ThrowIfNull(employees);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentOutOfRangeException.ThrowIfLessThan(query.Page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(query.PageSize, 1);

        return employees.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize);
    }
}
