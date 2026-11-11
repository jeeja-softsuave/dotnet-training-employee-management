using EmployeeManagement.Core.Models;

namespace EmployeeManagement.Tests;

/// <summary>Small, fixed data set so query results are easy to reason about in assertions.</summary>
internal static class TestData
{
    public static readonly DateOnly Hired = new(2022, 1, 10);

    public static FullTimeEmployee FullTime(int id = 1, string name = "Test Person", decimal salary = 50_000m,
        int departmentId = 1, string? email = "test@example.com", DateOnly? hireDate = null) =>
        new(id, name, salary, hireDate ?? Hired, departmentId, email);

    public static ContractEmployee Contract(int id = 1, string name = "Test Contractor", decimal salary = 40_000m,
        int departmentId = 1, DateOnly? endDate = null, string? email = null, DateOnly? hireDate = null) =>
        new(id, name, salary, hireDate ?? Hired, departmentId, endDate, email);

    public static List<Department> Departments() =>
    [
        new(1, "Engineering"),
        new(2, "Sales"),
        new(3, "Finance"),   // no employees
    ];

    /// <summary>
    /// Engineering: Alice 120k (FT), Bob 80k (FT), Carol 60k (contract, ends 2026-06-30)
    /// Sales:       Dan 80k (FT, no email), Eve 45k (contract, open-ended)
    /// </summary>
    public static List<Employee> Employees() =>
    [
        FullTime(1, "Alice Smith", 120_000m, 1, "alice@example.com", new DateOnly(2018, 4, 1)),
        FullTime(2, "Bob Jones", 80_000m, 1, "bob@example.com", new DateOnly(2021, 9, 15)),
        Contract(3, "Carol White", 60_000m, 1, new DateOnly(2026, 6, 30), "carol@example.com", new DateOnly(2024, 1, 1)),
        FullTime(4, "Dan Brown", 80_000m, 2, null, new DateOnly(2023, 3, 1)),
        Contract(5, "Eve Black", 45_000m, 2, null, "eve@example.com", new DateOnly(2025, 5, 20)),
    ];
}
