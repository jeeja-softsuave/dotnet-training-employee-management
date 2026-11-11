using EmployeeManagement.Core.Models;

namespace EmployeeManagement.Tests.Models;

// Naming: Member_Scenario_ExpectedResult. Every test follows Arrange-Act-Assert.
public class EmployeeTests
{
    // ---------- normal ----------

    [Fact]
    public void Constructor_ValidValues_SetsAllProperties()
    {
        // Arrange
        var hireDate = new DateOnly(2024, 1, 15);

        // Act
        var employee = new FullTimeEmployee(7, "Asha Menon", 50_000m, hireDate, 3, "asha@example.com");

        // Assert
        Assert.Equal(7, employee.Id);
        Assert.Equal("Asha Menon", employee.Name);
        Assert.Equal(50_000m, employee.Salary);
        Assert.Equal(hireDate, employee.HireDate);
        Assert.Equal(3, employee.DepartmentId);
        Assert.Equal("asha@example.com", employee.Email);
        Assert.Equal(EmploymentType.FullTime, employee.Type);
    }

    [Fact]
    public void Constructor_NameWithSurroundingSpaces_TrimsName()
    {
        var employee = TestData.FullTime(name: "   Ravi Kumar  ");

        Assert.Equal("Ravi Kumar", employee.Name);
    }

    [Fact]
    public void Constructor_NoEmail_EmailIsNull()
    {
        var employee = TestData.FullTime(email: null);

        Assert.Null(employee.Email);
    }

    [Fact]
    public void GiveRaise_TenPercent_IncreasesSalaryByTenPercent()
    {
        // Arrange
        var employee = TestData.FullTime(salary: 50_000m);

        // Act
        employee.GiveRaise(10m);

        // Assert
        Assert.Equal(55_000m, employee.Salary);
    }

    [Fact]
    public void GiveRaise_FractionalResult_RoundsToTwoDecimals()
    {
        var employee = TestData.FullTime(salary: 333.33m);

        employee.GiveRaise(3m);   // 343.3299 -> 343.33

        Assert.Equal(343.33m, employee.Salary);
    }

    [Fact]
    public void GiveRaise_ThroughSecondReference_VisibleThroughFirst()
    {
        // Employee is a reference type: both variables point at the same object.
        var original = TestData.FullTime(salary: 1_000m);
        var alias = original;

        alias.GiveRaise(50m);

        Assert.Equal(1_500m, original.Salary);
    }

    [Fact]
    public void CalculateAnnualCost_FullTime_AddsBenefits()
    {
        Employee employee = TestData.FullTime(salary: 100_000m);

        Assert.Equal(120_000m, employee.CalculateAnnualCost());
    }

    [Fact]
    public void CalculateAnnualCost_Contract_AddsAgencyFee()
    {
        Employee employee = TestData.Contract(salary: 100_000m);

        Assert.Equal(110_000m, employee.CalculateAnnualCost());
    }

    // ---------- boundary ----------

    [Theory]
    [InlineData(0.01)]
    [InlineData(10_000_000)]
    public void Constructor_SalaryAtBoundary_IsAccepted(decimal salary)
    {
        var employee = TestData.FullTime(salary: salary);

        Assert.Equal(salary, employee.Salary);
    }

    [Fact]
    public void Constructor_NameExactlyMaxLength_IsAccepted()
    {
        var name = new string('a', Employee.MaxNameLength);

        var employee = TestData.FullTime(name: name);

        Assert.Equal(Employee.MaxNameLength, employee.Name.Length);
    }

    [Fact]
    public void GiveRaise_HundredPercent_DoublesSalary()
    {
        var employee = TestData.FullTime(salary: 40_000m);

        employee.GiveRaise(100m);

        Assert.Equal(80_000m, employee.Salary);
    }

    [Theory]
    [InlineData("2025-01-09", 2)]   // day before 3rd anniversary
    [InlineData("2025-01-10", 3)]   // exactly on anniversary
    [InlineData("2022-01-10", 0)]   // hire date itself
    [InlineData("2021-12-31", 0)]   // before hire date
    public void YearsOfService_AroundAnniversary_CountsCompletedYears(string asOf, int expected)
    {
        var employee = TestData.FullTime(hireDate: new DateOnly(2022, 1, 10));

        var years = employee.YearsOfService(DateOnly.Parse(asOf));

        Assert.Equal(expected, years);
    }

    // ---------- invalid ----------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_BlankName_Throws(string? name)
    {
        Assert.Throws<ArgumentException>(() => TestData.FullTime(name: name!));
    }

    [Fact]
    public void Constructor_NameTooLong_Throws()
    {
        var name = new string('a', Employee.MaxNameLength + 1);

        Assert.Throws<ArgumentException>(() => TestData.FullTime(name: name));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10_000_000.01)]
    public void Constructor_SalaryOutOfRange_Throws(decimal salary)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TestData.FullTime(salary: salary));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Constructor_NonPositiveId_Throws(int id)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TestData.FullTime(id: id));
    }

    [Fact]
    public void Constructor_NonPositiveDepartmentId_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => TestData.FullTime(departmentId: 0));
    }

    [Fact]
    public void Constructor_DefaultHireDate_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new FullTimeEmployee(1, "Name", 1_000m, default, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(100.01)]
    public void GiveRaise_PercentageOutOfRange_ThrowsAndLeavesSalaryUnchanged(decimal percentage)
    {
        // Arrange
        var employee = TestData.FullTime(salary: 50_000m);

        // Act
        var act = () => employee.GiveRaise(percentage);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
        Assert.Equal(50_000m, employee.Salary);
    }

    [Fact]
    public void GiveRaise_ResultAboveMaxSalary_ThrowsAndLeavesSalaryUnchanged()
    {
        var employee = TestData.FullTime(salary: Employee.MaxSalary);

        Assert.Throws<ArgumentOutOfRangeException>(() => employee.GiveRaise(1m));
        Assert.Equal(Employee.MaxSalary, employee.Salary);
    }
}
