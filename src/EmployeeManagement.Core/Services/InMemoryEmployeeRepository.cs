using EmployeeManagement.Core.Exceptions;
using EmployeeManagement.Core.Models;

namespace EmployeeManagement.Core.Services;

/// <summary>
/// Dictionary-backed store: O(1) lookup, add and remove by id, versus O(n) for a List search.
/// </summary>
public sealed class InMemoryEmployeeRepository : IEmployeeRepository
{
    private readonly Dictionary<int, Employee> _employees = new();

    public int Count => _employees.Count;

    public void Add(Employee employee)
    {
        ArgumentNullException.ThrowIfNull(employee);

        if (!_employees.TryAdd(employee.Id, employee))
            throw new DuplicateEmployeeException(employee.Id);
    }

    public Employee? GetById(int id) => _employees.GetValueOrDefault(id);

    public IReadOnlyList<Employee> GetAll() => _employees.Values.OrderBy(e => e.Id).ToList();

    public bool Remove(int id) => _employees.Remove(id);
}
