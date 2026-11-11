using EmployeeManagement.Core.Formatting;
using EmployeeManagement.Core.Models;

namespace EmployeeManagement.Tests.Formatting;

public class EmployeeFormatterTests
{
    [Theory]
    [InlineData(0.01, "A")]
    [InlineData(49_999.99, "A")]
    [InlineData(50_000, "B")]
    [InlineData(99_999.99, "B")]
    [InlineData(100_000, "C")]
    [InlineData(150_000, "D")]
    public void SalaryBand_AtBandEdges_ReturnsExpectedBand(decimal salary, string expected)
    {
        Assert.Equal(expected, EmployeeFormatter.SalaryBand(salary));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SalaryBand_NonPositive_Throws(decimal salary)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EmployeeFormatter.SalaryBand(salary));
    }

    [Fact]
    public void Describe_OpenEndedContract()
    {
        var text = EmployeeFormatter.Describe(TestData.Contract(name: "Eve", endDate: null));

        Assert.Equal("Eve: open-ended contract", text);
    }

    [Fact]
    public void Describe_FixedTermContract_IncludesEndDate()
    {
        var text = EmployeeFormatter.Describe(TestData.Contract(name: "Carol", endDate: new DateOnly(2026, 6, 30)));

        Assert.Equal("Carol: contract until 2026-06-30", text);
    }

    [Theory]
    [InlineData(150_000, "Al: senior full-time (band D)")]
    [InlineData(80_000, "Al: full-time (band B)")]
    public void Describe_FullTime_UsesSalaryPattern(decimal salary, string expected)
    {
        Assert.Equal(expected, EmployeeFormatter.Describe(TestData.FullTime(name: "Al", salary: salary)));
    }

    [Fact]
    public void Describe_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => EmployeeFormatter.Describe(null!));
    }

    [Fact]
    public void BuildDepartmentReport_HasHeaderAndOneLinePerDepartment()
    {
        var summaries = new[]
        {
            new DepartmentSummary(1, "Engineering", 2, 200_000m, 100_000m),
            new DepartmentSummary(2, "Sales", 0, 0m, 0m),
        };

        var lines = EmployeeFormatter.BuildDepartmentReport(summaries)
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(4, lines.Length);   // header, separator, 2 rows
        Assert.StartsWith("Department", lines[0]);
        Assert.StartsWith("Engineering", lines[2]);
    }

    [Fact]
    public void BuildDepartmentReport_Empty_HasOnlyHeader()
    {
        var lines = EmployeeFormatter.BuildDepartmentReport([])
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(2, lines.Length);
    }
}
