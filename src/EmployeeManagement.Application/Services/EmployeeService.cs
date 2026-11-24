using EmployeeManagement.Application.Abstractions;
using EmployeeManagement.Application.Common;
using EmployeeManagement.Application.Dtos;
using EmployeeManagement.Application.Mapping;
using EmployeeManagement.Application.Security;
using EmployeeManagement.Core.Models;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Application.Services;

public interface IEmployeeService
{
    Task<PagedResult<EmployeeResponse>> SearchAsync(EmployeeQueryParameters query, CancellationToken cancellationToken = default);
    Task<EmployeeResponse> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<EmployeeResponse> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeResponse> PatchAsync(int id, PatchEmployeeRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<EmployeeResponse> GiveRaiseAsync(int id, RaiseRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Business rules for employees. Knows nothing about HTTP (no status codes, no controllers)
/// and nothing about EF Core (only the repository interfaces).
/// </summary>
public sealed class EmployeeService(
    IEmployeeRepository employees,
    IDepartmentRepository departments,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ILogger<EmployeeService> logger) : IEmployeeService
{
    public async Task<PagedResult<EmployeeResponse>> SearchAsync(EmployeeQueryParameters query, CancellationToken cancellationToken = default)
    {
        var page = await employees.SearchAsync(query, cancellationToken);
        return page.Map(e => e.ToResponse());
    }

    public async Task<EmployeeResponse> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var employee = await FindAsync(id, cancellationToken);
        return employee.ToResponse();
    }

    public async Task<EmployeeResponse> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var department = await RequireDepartmentAsync(request.DepartmentId, cancellationToken);
        var email = EmployeeMappings.NormalizeEmail(request.Email);
        await EnsureEmailIsFreeAsync(email, null, cancellationToken);

        // Id 0 = new; the database assigns the real id on SaveChanges.
        // Domain constructors re-check every rule (defence in depth).
        Employee employee = request.Type switch
        {
            EmploymentType.Contract => new ContractEmployee(0, request.Name, request.Salary, request.HireDate!.Value,
                request.DepartmentId, request.ContractEndDate, email),
            _ => new FullTimeEmployee(0, request.Name, request.Salary, request.HireDate!.Value,
                request.DepartmentId, email),
        };

        employees.Add(employee);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Employee {EmployeeId} ({EmploymentType}) created in department {DepartmentId} by {UserName}",
            employee.Id, employee.Type, employee.DepartmentId, currentUser.UserName);

        return employee.ToResponse(department.Name);
    }

    public async Task UpdateAsync(int id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var employee = await FindAsync(id, cancellationToken);
        await RequireDepartmentAsync(request.DepartmentId, cancellationToken);
        var email = EmployeeMappings.NormalizeEmail(request.Email);
        await EnsureEmailIsFreeAsync(email, id, cancellationToken);

        // PUT replaces all editable fields.
        employee.Name = request.Name;
        employee.Email = email;
        employee.ChangeSalary(request.Salary);
        employee.DepartmentId = request.DepartmentId;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Employee {EmployeeId} replaced (PUT) by {UserName}", id, currentUser.UserName);
    }

    public async Task<EmployeeResponse> PatchAsync(int id, PatchEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var employee = await FindAsync(id, cancellationToken);
        string? departmentName = null;

        // PATCH changes only what was sent.
        if (request.DepartmentId is { } departmentId)
        {
            departmentName = (await RequireDepartmentAsync(departmentId, cancellationToken)).Name;
            employee.DepartmentId = departmentId;
        }

        if (request.Email is not null)
        {
            var email = EmployeeMappings.NormalizeEmail(request.Email);
            await EnsureEmailIsFreeAsync(email, id, cancellationToken);
            employee.Email = email;
        }

        if (request.Name is not null)
            employee.Name = request.Name;

        if (request.Salary is { } salary)
            employee.ChangeSalary(salary);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Employee {EmployeeId} patched by {UserName}", id, currentUser.UserName);

        return employee.ToResponse(departmentName);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var employee = await FindAsync(id, cancellationToken);

        employees.Remove(employee);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Employee {EmployeeId} deleted by {UserName}", id, currentUser.UserName);
    }

    public async Task<EmployeeResponse> GiveRaiseAsync(int id, RaiseRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!RaiseApprovalPolicy.CanApprove(request.Percentage, currentUser.IsInRole))
        {
            var limit = RaiseApprovalPolicy.MaxApprovablePercentage(currentUser.IsInRole);
            logger.LogWarning("User {UserName} tried to approve a {Percentage}% raise for employee {EmployeeId} (limit {Limit}%)",
                currentUser.UserName, request.Percentage, id, limit);

            throw new ForbiddenException(limit == 0
                ? "You are not allowed to approve raises."
                : $"You can approve raises up to {limit}%. A {request.Percentage}% raise needs an Admin.");
        }

        var employee = await FindAsync(id, cancellationToken);
        var oldSalary = employee.Salary;

        employee.GiveRaise(request.Percentage);   // Week 1 domain rule: 0 < % <= 100, max salary
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Employee {EmployeeId} salary {OldSalary} -> {NewSalary} approved by {UserName}",
            id, oldSalary, employee.Salary, currentUser.UserName);

        return employee.ToResponse();
    }

    private async Task<Employee> FindAsync(int id, CancellationToken cancellationToken) =>
        await employees.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Employee", id);

    private async Task<Department> RequireDepartmentAsync(int departmentId, CancellationToken cancellationToken) =>
        await departments.GetByIdAsync(departmentId, cancellationToken)
        ?? throw new BusinessRuleException("DepartmentId", $"Department {departmentId} does not exist.");

    private async Task EnsureEmailIsFreeAsync(string? email, int? excludingEmployeeId, CancellationToken cancellationToken)
    {
        if (email is not null && await employees.EmailExistsAsync(email, excludingEmployeeId, cancellationToken))
            throw new ConflictException($"Another employee already uses the email '{email}'.");
    }
}
