namespace EmployeeManagement.Core.Models;

/// <summary>Contractor hired through an agency. No benefits, but the agency charges a fee.</summary>
public sealed class ContractEmployee : Employee
{
    public const decimal AgencyFeeRate = 0.10m;

    public ContractEmployee(int id, string name, decimal salary, DateOnly hireDate, int departmentId,
        DateOnly? contractEndDate = null, string? email = null)
        : base(id, name, salary, hireDate, departmentId, email)
    {
        if (contractEndDate is { } end && end <= hireDate)
            throw new ArgumentException("Contract end date must be after the hire date.", nameof(contractEndDate));

        ContractEndDate = contractEndDate;
    }

    /// <summary>Nullable value type (DateOnly?): null means an open-ended contract.</summary>
    public DateOnly? ContractEndDate { get; }

    public override EmploymentType Type => EmploymentType.Contract;

    public override decimal CalculateAnnualCost() =>
        Math.Round(Salary * (1 + AgencyFeeRate), 2, MidpointRounding.AwayFromZero);

    public bool IsActiveOn(DateOnly date) =>
        date >= HireDate && (ContractEndDate is null || date <= ContractEndDate);
}
