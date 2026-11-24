using EmployeeManagement.Application.Common;
using EmployeeManagement.Application.Dtos;
using EmployeeManagement.Core.Models;

namespace EmployeeManagement.Application.Abstractions;

// Persistence ports. The application layer defines WHAT it needs; the Infrastructure layer
// (EF Core) decides HOW. Dependencies point inwards: Infrastructure -> Application -> Core.
// (Week 1's synchronous in-memory repositories in Core are still used by the console app.)

public interface IEmployeeRepository
{
    /// <summary>Loads one employee with its department, tracked so changes can be saved.</summary>
    Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Filtered, sorted, paged, read-only list (no change tracking).</summary>
    Task<PagedResult<Employee>> SearchAsync(EmployeeQueryParameters query, CancellationToken cancellationToken = default);

    /// <summary>All employees, read-only. Used for department reports.</summary>
    Task<IReadOnlyList<Employee>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, int? excludingEmployeeId = null, CancellationToken cancellationToken = default);

    void Add(Employee employee);
    void Remove(Employee employee);
}

public interface IDepartmentRepository
{
    Task<Department?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DepartmentResponse>> ListWithHeadcountAsync(CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);
    Task<int> CountEmployeesAsync(int departmentId, CancellationToken cancellationToken = default);
    void Add(Department department);
    void Remove(Department department);
}

public interface IUserRepository
{
    Task<AppUser?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);
}

/// <summary>Commits all changes made through the repositories in one transaction.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
