using EmployeeManagement.Core.Models;

namespace EmployeeManagement.Core.Queries;

/// <summary>
/// LINQ queries as extension methods on IEnumerable&lt;Employee&gt;. Each one is a small pure function,
/// so it can be unit-tested with a plain list and chained: employees.InDepartment(1).TopEarners(3).
///
/// Methods returning IEnumerable use deferred execution: nothing runs until the result is enumerated
/// (foreach, ToList, Count...). Methods returning a list/dictionary/number execute immediately.
/// Argument checks run eagerly because these methods are not iterators (no yield).
/// </summary>
public static class EmployeeQueries
{
    // Q1 — Where. O(n).
    public static IEnumerable<Employee> InDepartment(this IEnumerable<Employee> employees, int departmentId)
    {
        ArgumentNullException.ThrowIfNull(employees);
        return employees.Where(e => e.DepartmentId == departmentId);
    }

    // Q2 — Where with a value-type predicate object. O(n).
    public static IEnumerable<Employee> WithSalaryIn(this IEnumerable<Employee> employees, SalaryRange range)
    {
        ArgumentNullException.ThrowIfNull(employees);
        return employees.Where(e => range.Contains(e.Salary));
    }

    // Q3 — case-insensitive search. O(n * m), m = name length.
    public static IEnumerable<Employee> SearchByName(this IEnumerable<Employee> employees, string? term)
    {
        ArgumentNullException.ThrowIfNull(employees);
        if (string.IsNullOrWhiteSpace(term))
            return [];

        var trimmed = term.Trim();
        return employees.Where(e => e.Name.Contains(trimmed, StringComparison.OrdinalIgnoreCase));
    }

    // Q4 — OrderByDescending + ThenBy (stable tie-break by name). O(n log n).
    public static IEnumerable<Employee> SortedBySalaryDescending(this IEnumerable<Employee> employees)
    {
        ArgumentNullException.ThrowIfNull(employees);
        return employees
            .OrderByDescending(e => e.Salary)
            .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase);
    }

    // Q5 — sort + Take. O(n log n).
    public static IEnumerable<Employee> TopEarners(this IEnumerable<Employee> employees, int count)
    {
        ArgumentNullException.ThrowIfNull(employees);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return employees.SortedBySalaryDescending().Take(count);
    }

    // Q6 — Where + OrderBy on dates. O(n log n).
    public static IEnumerable<Employee> HiredOnOrAfter(this IEnumerable<Employee> employees, DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(employees);
        return employees.Where(e => e.HireDate >= date).OrderBy(e => e.HireDate);
    }

    // Q7 — GroupBy + aggregates, with a Dictionary for O(1) name lookup. O(n + d log d).
    // Departments with no employees are included with zero headcount.
    public static IReadOnlyList<DepartmentSummary> SummarizeByDepartment(
        this IEnumerable<Employee> employees, IEnumerable<Department> departments)
    {
        ArgumentNullException.ThrowIfNull(employees);
        ArgumentNullException.ThrowIfNull(departments);

        var groups = employees
            .GroupBy(e => e.DepartmentId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return departments
            .Select(d =>
            {
                var members = groups.GetValueOrDefault(d.Id) ?? [];
                var total = members.Sum(e => e.Salary);
                var average = members.Count == 0 ? 0m : Math.Round(total / members.Count, 2, MidpointRounding.AwayFromZero);
                return new DepartmentSummary(d.Id, d.Name, members.Count, total, average);
            })
            .OrderByDescending(s => s.Headcount)
            .ThenBy(s => s.DepartmentName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // Q8 — Join (inner join: employees with an unknown department are dropped). O(n + d).
    public static IEnumerable<EmployeeDetails> WithDepartmentNames(
        this IEnumerable<Employee> employees, IEnumerable<Department> departments)
    {
        ArgumentNullException.ThrowIfNull(employees);
        ArgumentNullException.ThrowIfNull(departments);
        return employees.Join(
            departments,
            e => e.DepartmentId,
            d => d.Id,
            (e, d) => new EmployeeDetails(e.Id, e.Name, d.Name, e.Type, e.Salary));
    }

    // Q9 — GroupBy into a Dictionary. O(n).
    public static IReadOnlyDictionary<EmploymentType, int> HeadcountByType(this IEnumerable<Employee> employees)
    {
        ArgumentNullException.ThrowIfNull(employees);
        return employees.GroupBy(e => e.Type).ToDictionary(g => g.Key, g => g.Count());
    }

    // Q10 — data-quality check using nullable reference type. O(n).
    public static IEnumerable<Employee> MissingEmail(this IEnumerable<Employee> employees)
    {
        ArgumentNullException.ThrowIfNull(employees);
        return employees.Where(e => string.IsNullOrWhiteSpace(e.Email));
    }

    // Q11 — paging with Skip/Take (1-based page number). O(n log n) because of the sort.
    public static IEnumerable<Employee> Page(this IEnumerable<Employee> employees, int pageNumber, int pageSize)
    {
        ArgumentNullException.ThrowIfNull(employees);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        return employees.OrderBy(e => e.Id).Skip((pageNumber - 1) * pageSize).Take(pageSize);
    }

    // Q12 — Sum over a virtual method (polymorphism). O(n).
    public static decimal TotalAnnualCost(this IEnumerable<Employee> employees)
    {
        ArgumentNullException.ThrowIfNull(employees);
        return employees.Sum(e => e.CalculateAnnualCost());
    }

    // Q13 — OfType + property pattern. O(n log n).
    public static IEnumerable<ContractEmployee> ContractsEndingOnOrBefore(this IEnumerable<Employee> employees, DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(employees);
        return employees
            .OfType<ContractEmployee>()
            .Where(c => c.ContractEndDate is { } end && end <= date)
            .OrderBy(c => c.ContractEndDate);
    }
}
