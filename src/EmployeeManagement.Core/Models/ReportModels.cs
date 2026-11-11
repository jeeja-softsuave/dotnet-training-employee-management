namespace EmployeeManagement.Core.Models;

/// <summary>Result of grouping employees by department (LINQ GroupBy).</summary>
public sealed record DepartmentSummary(
    int DepartmentId,
    string DepartmentName,
    int Headcount,
    decimal TotalSalary,
    decimal AverageSalary);

/// <summary>Result of joining employees with departments (LINQ Join).</summary>
public sealed record EmployeeDetails(
    int Id,
    string Name,
    string DepartmentName,
    EmploymentType Type,
    decimal Salary);

/// <summary>Outcome of an async import.</summary>
public sealed record ImportResult(int Added, int Skipped);
