namespace EmployeeManagement.Core.Models;

/// <summary>
/// Record: immutable with value-based equality. Two Department instances with the same Id and Name
/// are Equal, which is what we want for reference data that never changes after creation.
/// Employee is a class instead, because employees have identity and mutable state (salary, department).
/// </summary>
public sealed record Department(int Id, string Name)
{
    // 0 = not saved yet: the database assigns the id on insert (Week 2, EF Core).
    public int Id { get; } = Id >= 0
        ? Id
        : throw new ArgumentOutOfRangeException(nameof(Id), Id, "Department id cannot be negative.");

    public string Name { get; } = !string.IsNullOrWhiteSpace(Name)
        ? Name.Trim()
        : throw new ArgumentException("Department name is required.", nameof(Name));
}
