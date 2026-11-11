using EmployeeManagement.Core.Models;

namespace EmployeeManagement.Core.Services;

public sealed class InMemoryDepartmentRepository : IDepartmentRepository
{
    private readonly Dictionary<int, Department> _departments = new();

    public void Add(Department department)
    {
        ArgumentNullException.ThrowIfNull(department);

        if (!_departments.TryAdd(department.Id, department))
            throw new ArgumentException($"A department with id {department.Id} already exists.", nameof(department));
    }

    public Department? GetById(int id) => _departments.GetValueOrDefault(id);

    public IReadOnlyList<Department> GetAll() => _departments.Values.OrderBy(d => d.Id).ToList();
}
