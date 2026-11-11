using System.Globalization;
using EmployeeManagement.Core.Exceptions;
using EmployeeManagement.Core.Formatting;
using EmployeeManagement.Core.Models;
using EmployeeManagement.Core.Queries;
using EmployeeManagement.Core.Services;

namespace EmployeeManagement.App;

/// <summary>
/// Menu-driven console UI. Reads from a TextReader and writes to a TextWriter instead of using
/// Console directly, so it can be driven by piped input (see README samples) or by tests.
/// </summary>
internal sealed class ConsoleUi(EmployeeService service, TextReader input, TextWriter output)
{
    private const string DateFormat = "yyyy-MM-dd";

    private static readonly string[] MenuItems =
    [
        "1. List all employees",
        "2. Add an employee",
        "3. Search by name",
        "4. Filter by department",
        "5. Filter by salary range",
        "6. Give a raise",
        "7. Remove an employee",
        "8. Department report",
        "9. Top earners",
        "0. Exit",
    ];

    /// <summary>Thrown internally when the input stream ends (e.g. piped input runs out).</summary>
    private sealed class EndOfInputException : Exception;

    public void Run()
    {
        try
        {
            while (true)
            {
                PrintMenu();
                var choice = ReadInt("Choose an option", 0, MenuItems.Length - 1);
                if (choice == 0)
                    break;

                try
                {
                    Execute(choice);
                }
                catch (Exception ex) when (ex is ArgumentException or EmployeeNotFoundException
                                               or DepartmentNotFoundException or DuplicateEmployeeException)
                {
                    // Expected business errors: show the message, keep the app running.
                    output.WriteLine($"Error: {ex.Message}");
                }
            }
        }
        catch (EndOfInputException)
        {
            output.WriteLine();
        }

        output.WriteLine("Goodbye.");
    }

    private void PrintMenu()
    {
        output.WriteLine();
        output.WriteLine("=== Employee Management ===");
        foreach (var item in MenuItems)
            output.WriteLine(item);
    }

    private void Execute(int choice)
    {
        switch (choice)
        {
            case 1: ListAll(); break;
            case 2: AddEmployee(); break;
            case 3: SearchByName(); break;
            case 4: FilterByDepartment(); break;
            case 5: FilterBySalary(); break;
            case 6: GiveRaise(); break;
            case 7: RemoveEmployee(); break;
            case 8: DepartmentReport(); break;
            case 9: TopEarners(); break;
        }
    }

    private void ListAll() => PrintEmployees(service.GetAllEmployees());

    private void AddEmployee()
    {
        var name = ReadRequired("Name");
        var isContract = ReadChoice("Type (F = full-time, C = contract)", "F", "C") == "C";
        var salary = ReadDecimal("Annual salary", 0.01m, Employee.MaxSalary);
        var hireDate = ReadDate("Hire date (yyyy-MM-dd, blank = today)", DateOnly.FromDateTime(DateTime.Today));
        PrintDepartments();
        var departmentId = ReadInt("Department id", 1, int.MaxValue);
        var email = ReadOptional("Email (optional)");

        var id = service.NextEmployeeId();
        Employee employee = isContract
            ? new ContractEmployee(id, name, salary, hireDate, departmentId,
                ReadOptionalDate("Contract end date (yyyy-MM-dd, blank = open-ended)"), email)
            : new FullTimeEmployee(id, name, salary, hireDate, departmentId, email);

        service.Hire(employee);
        output.WriteLine($"Added: {EmployeeFormatter.Describe(employee)}");
    }

    private void SearchByName()
    {
        var term = ReadRequired("Name contains");
        PrintEmployees(service.Search(term));
    }

    private void FilterByDepartment()
    {
        PrintDepartments();
        var departmentId = ReadInt("Department id", 1, int.MaxValue);
        PrintEmployees(service.GetAllEmployees().InDepartment(departmentId).ToList());
    }

    private void FilterBySalary()
    {
        var min = ReadDecimal("Minimum salary", 0m, Employee.MaxSalary);
        var max = ReadDecimal("Maximum salary", min, Employee.MaxSalary);
        var range = new SalaryRange(min, max);
        PrintEmployees(service.GetAllEmployees().WithSalaryIn(range).SortedBySalaryDescending().ToList());
    }

    private void GiveRaise()
    {
        var id = ReadInt("Employee id", 1, int.MaxValue);
        var percentage = ReadDecimal("Raise percentage (0-100]", 0.01m, 100m);
        service.GiveRaise(id, percentage);
    }

    private void RemoveEmployee()
    {
        var id = ReadInt("Employee id", 1, int.MaxValue);
        var employee = service.GetEmployee(id);
        service.Terminate(id);
        output.WriteLine($"Removed {employee.Name}.");
    }

    private void DepartmentReport()
    {
        var employees = service.GetAllEmployees();
        output.WriteLine(EmployeeFormatter.BuildDepartmentReport(service.GetDepartmentSummaries()));

        var byType = employees.HeadcountByType();
        output.WriteLine($"Full-time: {byType.GetValueOrDefault(EmploymentType.FullTime)}, " +
                         $"Contract: {byType.GetValueOrDefault(EmploymentType.Contract)}");
        output.WriteLine($"Total annual cost (salary + benefits/fees): {employees.TotalAnnualCost():N2}");

        var missingEmail = employees.MissingEmail().Select(e => e.Name).ToList();
        if (missingEmail.Count > 0)
            output.WriteLine($"Missing email: {string.Join(", ", missingEmail)}");
    }

    private void TopEarners()
    {
        var count = ReadInt("How many", 1, 100);
        var departments = service.GetAllDepartments();
        var top = service.GetAllEmployees().TopEarners(count).WithDepartmentNames(departments);
        var rank = 1;
        foreach (var e in top)
            output.WriteLine($"{rank++}. {e.Name,-20} {e.DepartmentName,-16} {e.Salary,12:N2}");
    }

    // ---------- output helpers ----------

    private void PrintEmployees(IReadOnlyCollection<Employee> employees)
    {
        if (employees.Count == 0)
        {
            output.WriteLine("No employees found.");
            return;
        }

        output.WriteLine($"{"Id",-5} {"Name",-20} {"Dept",-8} {"Type",-9} {"Salary",12}");
        foreach (var e in employees)
            output.WriteLine(e);
        output.WriteLine($"({employees.Count} employee(s))");
    }

    private void PrintDepartments()
    {
        var list = string.Join(", ", service.GetAllDepartments().Select(d => $"{d.Id} = {d.Name}"));
        output.WriteLine($"Departments: {list}");
    }

    // ---------- input helpers: re-prompt until the input is valid ----------

    private string ReadLine(string prompt)
    {
        output.Write($"{prompt}: ");
        var line = input.ReadLine() ?? throw new EndOfInputException();
        return line.Trim();
    }

    private string ReadRequired(string prompt)
    {
        while (true)
        {
            var value = ReadLine(prompt);
            if (value.Length > 0)
                return value;
            output.WriteLine("  A value is required.");
        }
    }

    private string? ReadOptional(string prompt)
    {
        var value = ReadLine(prompt);
        return value.Length == 0 ? null : value;
    }

    private string ReadChoice(string prompt, params string[] allowed)
    {
        while (true)
        {
            var value = ReadLine(prompt).ToUpperInvariant();
            if (allowed.Contains(value))
                return value;
            output.WriteLine($"  Please enter one of: {string.Join(", ", allowed)}.");
        }
    }

    private int ReadInt(string prompt, int min, int max)
    {
        while (true)
        {
            var text = ReadLine(prompt);
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value >= min && value <= max)
                return value;
            output.WriteLine($"  Please enter a whole number between {min} and {max}.");
        }
    }

    private decimal ReadDecimal(string prompt, decimal min, decimal max)
    {
        while (true)
        {
            var text = ReadLine(prompt);
            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) && value >= min && value <= max)
                return value;
            output.WriteLine($"  Please enter a number between {min:N2} and {max:N2}.");
        }
    }

    private DateOnly ReadDate(string prompt, DateOnly defaultValue)
    {
        while (true)
        {
            var text = ReadLine(prompt);
            if (text.Length == 0)
                return defaultValue;
            if (DateOnly.TryParseExact(text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return date;
            output.WriteLine($"  Please enter a date in the format {DateFormat}.");
        }
    }

    private DateOnly? ReadOptionalDate(string prompt)
    {
        while (true)
        {
            var text = ReadLine(prompt);
            if (text.Length == 0)
                return null;
            if (DateOnly.TryParseExact(text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return date;
            output.WriteLine($"  Please enter a date in the format {DateFormat}.");
        }
    }
}
