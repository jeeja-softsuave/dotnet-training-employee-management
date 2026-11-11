namespace EmployeeManagement.Core.Models;

/// <summary>Permanent employee. Company pays benefits (insurance, provident fund) on top of salary.</summary>
public sealed class FullTimeEmployee : Employee
{
    public const decimal BenefitsRate = 0.20m;

    public FullTimeEmployee(int id, string name, decimal salary, DateOnly hireDate, int departmentId, string? email = null)
        : base(id, name, salary, hireDate, departmentId, email)
    {
    }

    public override EmploymentType Type => EmploymentType.FullTime;

    public override decimal CalculateAnnualCost() =>
        Math.Round(Salary * (1 + BenefitsRate), 2, MidpointRounding.AwayFromZero);
}
