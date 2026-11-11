using EmployeeManagement.Core.Models;

namespace EmployeeManagement.Core.Services;

/// <summary>
/// Somewhere employees can be fetched from asynchronously (a file, an HTTP API, a database).
/// Takes a CancellationToken so a slow fetch can be abandoned.
/// </summary>
public interface IEmployeeDataSource
{
    Task<IReadOnlyList<Employee>> FetchEmployeesAsync(CancellationToken cancellationToken = default);
}
