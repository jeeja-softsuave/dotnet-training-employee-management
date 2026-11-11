using EmployeeManagement.Core.Models;

namespace EmployeeManagement.Core.Services;

public static class SampleData
{
    public static IReadOnlyList<Department> CreateDepartments() =>
    [
        new Department(1, "Engineering"),
        new Department(2, "Human Resources"),
        new Department(3, "Sales"),
        new Department(4, "Finance"),   // intentionally empty: shows up with no employees in reports
    ];

    /// <summary>
    /// 10 valid employees plus 1 that references a department that does not exist,
    /// so the import shows how invalid records are skipped.
    /// </summary>
    public static IReadOnlyList<Employee> CreateEmployees() =>
    [
        new FullTimeEmployee(1, "Asha Menon", 95_000m, new DateOnly(2019, 3, 11), 1, "asha.menon@example.com"),
        new FullTimeEmployee(2, "Ravi Kumar", 72_000m, new DateOnly(2021, 7, 1), 1, "ravi.kumar@example.com"),
        new ContractEmployee(3, "Priya Nair", 60_000m, new DateOnly(2024, 2, 15), 1, new DateOnly(2027, 2, 14)),
        new FullTimeEmployee(4, "Daniel Joseph", 54_000m, new DateOnly(2018, 9, 3), 2, "daniel.joseph@example.com"),
        new FullTimeEmployee(5, "Meera Pillai", 48_000m, new DateOnly(2023, 1, 9), 2),
        new FullTimeEmployee(6, "Arjun Das", 88_000m, new DateOnly(2020, 5, 18), 3, "arjun.das@example.com"),
        new ContractEmployee(7, "Fatima Khan", 45_000m, new DateOnly(2025, 6, 1), 3, new DateOnly(2026, 12, 31), "fatima.khan@example.com"),
        new FullTimeEmployee(8, "Vikram Rao", 160_000m, new DateOnly(2015, 11, 23), 1, "vikram.rao@example.com"),
        new FullTimeEmployee(9, "Sneha Iyer", 66_000m, new DateOnly(2022, 10, 10), 3, "sneha.iyer@example.com"),
        new ContractEmployee(10, "Thomas Mathew", 52_000m, new DateOnly(2025, 9, 1), 1, email: "thomas.mathew@example.com"),
        new FullTimeEmployee(11, "Ghost Record", 50_000m, new DateOnly(2024, 1, 1), 99),
    ];
}
