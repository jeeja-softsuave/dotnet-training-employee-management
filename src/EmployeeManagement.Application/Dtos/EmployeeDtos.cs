using System.ComponentModel.DataAnnotations;
using EmployeeManagement.Core.Models;

namespace EmployeeManagement.Application.Dtos;

// DTOs (Data Transfer Objects) are the API's public contract. They are separate from the
// domain entities so the JSON shape can stay stable while the entities change, and so
// clients can never set fields they shouldn't (like Id or AnnualCost).
//
// DataAnnotations check the SHAPE of a request (required, length, range). [ApiController]
// runs them automatically and returns 400 before the action executes. Rules that need the
// database (department exists, email unique) live in the services.

/// <summary>An employee as returned by the API.</summary>
public sealed record EmployeeResponse(
    int Id,
    string Name,
    string? Email,
    EmploymentType Type,
    decimal Salary,
    string SalaryBand,
    decimal AnnualCost,
    DateOnly HireDate,
    DateOnly? ContractEndDate,
    int DepartmentId,
    string DepartmentName);

/// <summary>POST /api/employees</summary>
public sealed class CreateEmployeeRequest : IValidatableObject
{
    [Required, StringLength(Employee.MaxNameLength, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [EmailAddress, StringLength(254)]
    public string? Email { get; set; }

    [Required]
    public EmploymentType? Type { get; set; }

    [Range(0.01, (double)Employee.MaxSalary)]
    public decimal Salary { get; set; }

    [Required]
    public DateOnly? HireDate { get; set; }

    [Range(1, int.MaxValue)]
    public int DepartmentId { get; set; }

    /// <summary>Only for contract employees. Leave empty for an open-ended contract.</summary>
    public DateOnly? ContractEndDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name))
            yield return new ValidationResult("Name cannot be blank.", [nameof(Name)]);

        if (ContractEndDate is not null && Type != EmploymentType.Contract)
            yield return new ValidationResult("Only contract employees can have a contract end date.", [nameof(ContractEndDate)]);

        if (ContractEndDate is { } end && HireDate is { } hired && end <= hired)
            yield return new ValidationResult("Contract end date must be after the hire date.", [nameof(ContractEndDate)]);
    }
}

/// <summary>
/// PUT /api/employees/{id}: replaces every editable field. A field left out is treated as
/// "set to empty" (e.g. Email = null removes the email). Type, hire date and contract end date
/// are fixed when the employee is created.
/// </summary>
public sealed class UpdateEmployeeRequest : IValidatableObject
{
    [Required, StringLength(Employee.MaxNameLength, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [EmailAddress, StringLength(254)]
    public string? Email { get; set; }

    [Range(0.01, (double)Employee.MaxSalary)]
    public decimal Salary { get; set; }

    [Range(1, int.MaxValue)]
    public int DepartmentId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name))
            yield return new ValidationResult("Name cannot be blank.", [nameof(Name)]);
    }
}

/// <summary>
/// PATCH /api/employees/{id}: changes only the fields that are sent. A missing (null) field
/// means "leave as is". At least one field is required.
/// </summary>
public sealed class PatchEmployeeRequest : IValidatableObject
{
    [StringLength(Employee.MaxNameLength, MinimumLength = 2)]
    public string? Name { get; set; }

    [EmailAddress, StringLength(254)]
    public string? Email { get; set; }

    [Range(0.01, (double)Employee.MaxSalary)]
    public decimal? Salary { get; set; }

    [Range(1, int.MaxValue)]
    public int? DepartmentId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Name is null && Email is null && Salary is null && DepartmentId is null)
            yield return new ValidationResult("Send at least one field to change.", [nameof(Name), nameof(Email), nameof(Salary), nameof(DepartmentId)]);

        if (Name is not null && string.IsNullOrWhiteSpace(Name))
            yield return new ValidationResult("Name cannot be blank.", [nameof(Name)]);
    }
}

/// <summary>POST /api/employees/{id}/raise</summary>
public sealed class RaiseRequest
{
    [Range(0.01, 100)]
    public decimal Percentage { get; set; }
}
