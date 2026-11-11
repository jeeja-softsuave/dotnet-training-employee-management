using EmployeeManagement.Core.Models;

namespace EmployeeManagement.Core.Services;

/// <summary>
/// Storage abstraction for employees. Callers depend on this interface, not on a concrete class
/// (Dependency Inversion), so the in-memory version can later be swapped for EF Core / SQL.
/// </summary>
public interface IEmployeeRepository
{
    int Count { get; }

    /// <exception cref="Exceptions.DuplicateEmployeeException">An employee with the same id exists.</exception>
    void Add(Employee employee);

    Employee? GetById(int id);

    /// <summary>All employees ordered by id.</summary>
    IReadOnlyList<Employee> GetAll();

    /// <returns><c>true</c> if an employee was removed; <c>false</c> if the id was not found.</returns>
    bool Remove(int id);
}
