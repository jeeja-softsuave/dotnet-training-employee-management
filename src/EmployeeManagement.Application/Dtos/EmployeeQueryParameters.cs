using System.ComponentModel.DataAnnotations;
using EmployeeManagement.Core.Models;

namespace EmployeeManagement.Application.Dtos;

/// <summary>
/// Query string for GET /api/employees, e.g.
/// <c>?departmentId=1&amp;type=FullTime&amp;minSalary=50000&amp;search=kumar&amp;sortBy=salary&amp;sortDirection=desc&amp;page=2&amp;pageSize=5</c>
/// </summary>
public sealed class EmployeeQueryParameters : IValidatableObject
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;

    public static readonly IReadOnlyList<string> SortableFields = ["id", "name", "salary", "hireDate"];

    [Range(1, int.MaxValue)]
    public int? DepartmentId { get; set; }

    public EmploymentType? Type { get; set; }

    [Range(0, (double)Employee.MaxSalary)]
    public decimal? MinSalary { get; set; }

    [Range(0, (double)Employee.MaxSalary)]
    public decimal? MaxSalary { get; set; }

    /// <summary>Case-insensitive match on name or email.</summary>
    [StringLength(100)]
    public string? Search { get; set; }

    /// <summary>One of: id, name, salary, hireDate.</summary>
    public string SortBy { get; set; } = "id";

    /// <summary>asc or desc.</summary>
    public string SortDirection { get; set; } = "asc";

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize { get; set; } = DefaultPageSize;

    public bool IsDescending => string.Equals(SortDirection, "desc", StringComparison.OrdinalIgnoreCase);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinSalary is { } min && MaxSalary is { } max && min > max)
            yield return new ValidationResult("minSalary cannot be greater than maxSalary.", [nameof(MinSalary), nameof(MaxSalary)]);

        if (!SortableFields.Contains(SortBy, StringComparer.OrdinalIgnoreCase))
            yield return new ValidationResult($"sortBy must be one of: {string.Join(", ", SortableFields)}.", [nameof(SortBy)]);

        if (!string.Equals(SortDirection, "asc", StringComparison.OrdinalIgnoreCase) && !IsDescending)
            yield return new ValidationResult("sortDirection must be 'asc' or 'desc'.", [nameof(SortDirection)]);
    }
}
