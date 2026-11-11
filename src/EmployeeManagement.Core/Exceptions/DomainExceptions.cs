namespace EmployeeManagement.Core.Exceptions;

public sealed class EmployeeNotFoundException(int employeeId)
    : Exception($"Employee with id {employeeId} was not found.")
{
    public int EmployeeId { get; } = employeeId;
}

public sealed class DepartmentNotFoundException(int departmentId)
    : Exception($"Department with id {departmentId} was not found.")
{
    public int DepartmentId { get; } = departmentId;
}

public sealed class DuplicateEmployeeException(int employeeId)
    : Exception($"An employee with id {employeeId} already exists.")
{
    public int EmployeeId { get; } = employeeId;
}
