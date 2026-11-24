using EmployeeManagement.Application.Dtos;
using EmployeeManagement.Core.Formatting;
using EmployeeManagement.Core.Models;

namespace EmployeeManagement.Application.Mapping;

/// <summary>
/// Manual entity -> DTO mapping. Chosen over AutoMapper because it is explicit, checked by the
/// compiler, easy to debug, and the mappings here are small.
/// </summary>
public static class EmployeeMappings
{
    public static EmployeeResponse ToResponse(this Employee employee, string? departmentName = null) => new(
        employee.Id,
        employee.Name,
        employee.Email,
        employee.Type,
        employee.Salary,
        EmployeeFormatter.SalaryBand(employee.Salary),     // reuses Week 1 pattern matching
        employee.CalculateAnnualCost(),                    // reuses Week 1 polymorphism
        employee.HireDate,
        (employee as ContractEmployee)?.ContractEndDate,
        employee.DepartmentId,
        departmentName ?? employee.Department?.Name ?? string.Empty);

    /// <summary>Trim and lower-case emails so uniqueness checks are case-insensitive.</summary>
    public static string? NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
}
