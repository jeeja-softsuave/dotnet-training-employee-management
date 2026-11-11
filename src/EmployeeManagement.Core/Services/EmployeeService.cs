using EmployeeManagement.Core.Exceptions;
using EmployeeManagement.Core.Models;
using EmployeeManagement.Core.Queries;

namespace EmployeeManagement.Core.Services;

/// <summary>
/// Application logic (business rules) on top of the repositories.
/// Composition: the service HAS repositories (injected through the constructor) instead of
/// inheriting from them, so they can be replaced with fakes in tests or a database later.
/// </summary>
public sealed class EmployeeService
{
    private readonly IEmployeeRepository _employees;
    private readonly IDepartmentRepository _departments;

    public EmployeeService(IEmployeeRepository employees, IDepartmentRepository departments)
    {
        _employees = employees ?? throw new ArgumentNullException(nameof(employees));
        _departments = departments ?? throw new ArgumentNullException(nameof(departments));
    }

    /// <summary>Raised after an employee is added (by Hire or ImportAsync).</summary>
    public event EventHandler<EmployeeEventArgs>? EmployeeAdded;

    /// <summary>Raised after a successful raise.</summary>
    public event EventHandler<SalaryChangedEventArgs>? SalaryChanged;

    public IReadOnlyList<Employee> GetAllEmployees() => _employees.GetAll();

    public IReadOnlyList<Department> GetAllDepartments() => _departments.GetAll();

    public Employee GetEmployee(int id) => _employees.GetById(id) ?? throw new EmployeeNotFoundException(id);

    public int NextEmployeeId() => _employees.Count == 0 ? 1 : _employees.GetAll().Max(e => e.Id) + 1;

    public void Hire(Employee employee)
    {
        ArgumentNullException.ThrowIfNull(employee);

        if (_departments.GetById(employee.DepartmentId) is null)
            throw new DepartmentNotFoundException(employee.DepartmentId);

        _employees.Add(employee);
        EmployeeAdded?.Invoke(this, new EmployeeEventArgs(employee));
    }

    public void GiveRaise(int employeeId, decimal percentage)
    {
        var employee = GetEmployee(employeeId);
        var oldSalary = employee.Salary;

        employee.GiveRaise(percentage);

        SalaryChanged?.Invoke(this, new SalaryChangedEventArgs(employee, oldSalary, employee.Salary));
    }

    public void Terminate(int employeeId)
    {
        if (!_employees.Remove(employeeId))
            throw new EmployeeNotFoundException(employeeId);
    }

    public IReadOnlyList<Employee> Search(string? term) => _employees.GetAll().SearchByName(term).ToList();

    public IReadOnlyList<DepartmentSummary> GetDepartmentSummaries() =>
        _employees.GetAll().SummarizeByDepartment(_departments.GetAll());

    /// <summary>
    /// Fetches employees from <paramref name="source"/> and adds every valid one.
    /// Records with a duplicate id or an unknown department are skipped, not fatal.
    /// </summary>
    /// <exception cref="OperationCanceledException">The token was cancelled before the import finished.</exception>
    public async Task<ImportResult> ImportAsync(IEmployeeDataSource source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        var fetched = await source.FetchEmployeesAsync(cancellationToken).ConfigureAwait(false);

        int added = 0, skipped = 0;
        foreach (var employee in fetched)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_employees.GetById(employee.Id) is not null || _departments.GetById(employee.DepartmentId) is null)
            {
                skipped++;
                continue;
            }

            _employees.Add(employee);
            added++;
            EmployeeAdded?.Invoke(this, new EmployeeEventArgs(employee));
        }

        return new ImportResult(added, skipped);
    }
}
