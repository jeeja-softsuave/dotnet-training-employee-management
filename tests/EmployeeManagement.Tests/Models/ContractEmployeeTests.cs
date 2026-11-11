using EmployeeManagement.Core.Models;

namespace EmployeeManagement.Tests.Models;

public class ContractEmployeeTests
{
    [Fact]
    public void Constructor_EndDateAfterHireDate_SetsEndDate()
    {
        var end = TestData.Hired.AddYears(1);

        var contractor = TestData.Contract(endDate: end);

        Assert.Equal(end, contractor.ContractEndDate);
        Assert.Equal(EmploymentType.Contract, contractor.Type);
    }

    [Fact]
    public void Constructor_NoEndDate_IsOpenEnded()
    {
        var contractor = TestData.Contract(endDate: null);

        Assert.Null(contractor.ContractEndDate);
    }

    [Theory]
    [InlineData(0)]    // same day as hire
    [InlineData(-1)]   // before hire
    public void Constructor_EndDateNotAfterHireDate_Throws(int daysFromHire)
    {
        var end = TestData.Hired.AddDays(daysFromHire);

        Assert.Throws<ArgumentException>(() => TestData.Contract(endDate: end));
    }

    [Theory]
    [InlineData("2022-01-09", false)]   // before hire
    [InlineData("2022-01-10", true)]    // hire day
    [InlineData("2022-12-31", true)]    // last day of contract
    [InlineData("2023-01-01", false)]   // day after contract ends
    public void IsActiveOn_AroundContractDates_ReturnsExpected(string date, bool expected)
    {
        var contractor = TestData.Contract(endDate: new DateOnly(2022, 12, 31));

        Assert.Equal(expected, contractor.IsActiveOn(DateOnly.Parse(date)));
    }

    [Fact]
    public void IsActiveOn_OpenEndedFarFuture_ReturnsTrue()
    {
        var contractor = TestData.Contract(endDate: null);

        Assert.True(contractor.IsActiveOn(new DateOnly(2099, 1, 1)));
    }
}
