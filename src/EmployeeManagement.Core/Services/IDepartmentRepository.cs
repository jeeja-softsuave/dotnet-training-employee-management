using EmployeeManagement.Core.Models;

namespace EmployeeManagement.Core.Services;

public interface IDepartmentRepository
{
    void Add(Department department);
    Department? GetById(int id);
    IReadOnlyList<Department> GetAll();
}
