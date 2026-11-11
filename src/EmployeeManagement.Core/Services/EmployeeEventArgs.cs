using EmployeeManagement.Core.Models;

namespace EmployeeManagement.Core.Services;

public sealed class EmployeeEventArgs(Employee employee) : EventArgs
{
    public Employee Employee { get; } = employee;
}

public sealed class SalaryChangedEventArgs(Employee employee, decimal oldSalary, decimal newSalary) : EventArgs
{
    public Employee Employee { get; } = employee;
    public decimal OldSalary { get; } = oldSalary;
    public decimal NewSalary { get; } = newSalary;
}
