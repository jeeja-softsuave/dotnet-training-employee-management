using EmployeeManagement.Application.Abstractions;
using EmployeeManagement.Application.Common;
using EmployeeManagement.Application.Dtos;
using EmployeeManagement.Core.Models;
using EmployeeManagement.Core.Queries;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Application.Services;

public interface IDepartmentService
{
    Task<IReadOnlyList<DepartmentResponse>> ListAsync(CancellationToken cancellationToken = default);
    Task<DepartmentResponse> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<DepartmentResponse> CreateAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DepartmentSummary>> GetSummaryAsync(CancellationToken cancellationToken = default);
}

public sealed class DepartmentService(
    IDepartmentRepository departments,
    IEmployeeRepository employees,
    IUnitOfWork unitOfWork,
    ILogger<DepartmentService> logger) : IDepartmentService
{
    public Task<IReadOnlyList<DepartmentResponse>> ListAsync(CancellationToken cancellationToken = default) =>
        departments.ListWithHeadcountAsync(cancellationToken);

    public async Task<DepartmentResponse> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var department = await departments.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Department", id);
        var headcount = await departments.CountEmployeesAsync(id, cancellationToken);
        return new DepartmentResponse(department.Id, department.Name, headcount);
    }

    public async Task<DepartmentResponse> CreateAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var name = request.Name.Trim();
        if (await departments.NameExistsAsync(name, cancellationToken))
            throw new ConflictException($"A department named '{name}' already exists.");

        var department = new Department(0, name);
        departments.Add(department);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Department {DepartmentId} '{DepartmentName}' created", department.Id, department.Name);
        return new DepartmentResponse(department.Id, department.Name, 0);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var department = await departments.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Department", id);

        var headcount = await departments.CountEmployeesAsync(id, cancellationToken);
        if (headcount > 0)
            throw new ConflictException($"Department '{department.Name}' still has {headcount} employee(s). Move or remove them first.");

        departments.Remove(department);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Department {DepartmentId} deleted", id);
    }

    /// <summary>Headcount and salary totals per department, reusing the Week 1 LINQ query.</summary>
    public async Task<IReadOnlyList<DepartmentSummary>> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var all = await employees.GetAllAsync(cancellationToken);
        var depts = await departments.ListWithHeadcountAsync(cancellationToken);

        return all.SummarizeByDepartment(depts.Select(d => new Department(d.Id, d.Name)));
    }
}
