using System.Text;
using EmployeeManagement.Core.Models;

namespace EmployeeManagement.Core.Formatting;

public static class EmployeeFormatter
{
    /// <summary>Relational pattern matching on a decimal.</summary>
    public static string SalaryBand(decimal salary) => salary switch
    {
        <= 0 => throw new ArgumentOutOfRangeException(nameof(salary), salary, "Salary must be positive."),
        < 50_000m => "A",
        < 100_000m => "B",
        < 150_000m => "C",
        _ => "D",
    };

    /// <summary>Type and property patterns in a switch expression.</summary>
    public static string Describe(Employee employee) => employee switch
    {
        null => throw new ArgumentNullException(nameof(employee)),
        ContractEmployee { ContractEndDate: null } => $"{employee.Name}: open-ended contract",
        ContractEmployee { ContractEndDate: { } end } => $"{employee.Name}: contract until {end:yyyy-MM-dd}",
        FullTimeEmployee { Salary: >= 150_000m } => $"{employee.Name}: senior full-time (band {SalaryBand(employee.Salary)})",
        FullTimeEmployee => $"{employee.Name}: full-time (band {SalaryBand(employee.Salary)})",
        _ => $"{employee.Name}: {employee.Type}",
    };

    /// <summary>
    /// Builds a text table. StringBuilder is used because the string is built in a loop:
    /// string is immutable, so "+=" in a loop would allocate a new string every iteration.
    /// </summary>
    public static string BuildDepartmentReport(IEnumerable<DepartmentSummary> summaries)
    {
        ArgumentNullException.ThrowIfNull(summaries);

        var sb = new StringBuilder();
        sb.AppendLine($"{"Department",-18} {"Headcount",9} {"Total salary",15} {"Average",12}");
        sb.AppendLine(new string('-', 57));

        foreach (var s in summaries)
            sb.AppendLine($"{s.DepartmentName,-18} {s.Headcount,9} {s.TotalSalary,15:N2} {s.AverageSalary,12:N2}");

        return sb.ToString();
    }
}
