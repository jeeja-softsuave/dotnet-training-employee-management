namespace EmployeeManagement.Core.Models;

/// <summary>
/// Base class for every employee. Abstract because "an employee" is always a concrete kind
/// (full-time or contract) and each kind calculates its annual cost differently (polymorphism).
/// Reference type: two variables can point at the same Employee object, so a raise given through
/// one variable is visible through the other.
/// </summary>
public abstract class Employee
{
    public const int MaxNameLength = 100;
    public const decimal MaxSalary = 10_000_000m;

    private string _name = string.Empty;
    private decimal _salary;
    private int _departmentId;

    protected Employee(int id, string name, decimal salary, DateOnly hireDate, int departmentId, string? email = null)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id), id, "Employee id must be a positive number.");
        if (hireDate == default)
            throw new ArgumentException("Hire date is required.", nameof(hireDate));

        Id = id;
        Name = name;
        Salary = salary;
        HireDate = hireDate;
        DepartmentId = departmentId;
        Email = email;
    }

    public int Id { get; }

    public string Name
    {
        get => _name;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Name is required.", nameof(Name));
            var trimmed = value.Trim();
            if (trimmed.Length > MaxNameLength)
                throw new ArgumentException($"Name cannot be longer than {MaxNameLength} characters.", nameof(Name));
            _name = trimmed;
        }
    }

    /// <summary>Annual salary. Changed only through <see cref="GiveRaise"/> so the rules live in one place (encapsulation).</summary>
    public decimal Salary
    {
        get => _salary;
        private set
        {
            if (value <= 0 || value > MaxSalary)
                throw new ArgumentOutOfRangeException(nameof(Salary), value, $"Salary must be greater than 0 and at most {MaxSalary:N0}.");
            _salary = value;
        }
    }

    public DateOnly HireDate { get; }

    public int DepartmentId
    {
        get => _departmentId;
        set
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(DepartmentId), value, "Department id must be a positive number.");
            _departmentId = value;
        }
    }

    /// <summary>Nullable reference type: an employee may not have an email address yet.</summary>
    public string? Email { get; set; }

    public abstract EmploymentType Type { get; }

    /// <summary>Total yearly cost to the company. Each subclass adds its own overhead.</summary>
    public abstract decimal CalculateAnnualCost();

    /// <summary>Increase salary by a percentage in the range (0, 100].</summary>
    public void GiveRaise(decimal percentage)
    {
        if (percentage <= 0 || percentage > 100)
            throw new ArgumentOutOfRangeException(nameof(percentage), percentage, "Raise percentage must be greater than 0 and at most 100.");

        Salary = Math.Round(Salary * (1 + percentage / 100m), 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>Completed years of service on <paramref name="asOf"/>. Takes the date as a parameter so tests are deterministic.</summary>
    public int YearsOfService(DateOnly asOf)
    {
        if (asOf < HireDate)
            return 0;

        var years = asOf.Year - HireDate.Year;
        if (asOf < HireDate.AddYears(years))
            years--;
        return years;
    }

    public override string ToString() =>
        $"#{Id,-4} {Name,-20} Dept {DepartmentId,-3} {Type,-9} {Salary,12:N2}";
}
