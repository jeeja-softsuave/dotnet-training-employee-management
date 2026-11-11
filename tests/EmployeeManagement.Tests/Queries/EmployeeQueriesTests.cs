using EmployeeManagement.Core.Models;
using EmployeeManagement.Core.Queries;

namespace EmployeeManagement.Tests.Queries;

public class EmployeeQueriesTests
{
    private readonly List<Employee> _employees = TestData.Employees();
    private readonly List<Department> _departments = TestData.Departments();

    private static int[] Ids(IEnumerable<Employee> employees) => employees.Select(e => e.Id).ToArray();

    // Q1
    [Fact]
    public void InDepartment_Engineering_ReturnsOnlyEngineers()
    {
        var result = _employees.InDepartment(1);

        Assert.Equal([1, 2, 3], Ids(result));
    }

    [Fact]
    public void InDepartment_NoMembers_ReturnsEmpty()
    {
        Assert.Empty(_employees.InDepartment(3));
    }

    [Fact]
    public void InDepartment_NullSource_Throws()
    {
        List<Employee>? source = null;

        Assert.Throws<ArgumentNullException>(() => source!.InDepartment(1));
    }

    // Q2
    [Fact]
    public void WithSalaryIn_MidRange_ReturnsEmployeesInside()
    {
        var result = _employees.WithSalaryIn(new SalaryRange(50_000m, 100_000m));

        Assert.Equal([2, 3, 4], Ids(result));
    }

    // Q3
    [Theory]
    [InlineData("alice", new[] { 1 })]
    [InlineData("  BLACK ", new[] { 5 })]
    [InlineData("o", new[] { 2, 3, 4 })]   // Bob Jones, Carol White, Dan Brown
    public void SearchByName_CaseInsensitive_ReturnsMatches(string term, int[] expected)
    {
        var result = _employees.SearchByName(term);

        Assert.Equal(expected.Order(), Ids(result).Order());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SearchByName_BlankTerm_ReturnsEmpty(string? term)
    {
        Assert.Empty(_employees.SearchByName(term));
    }

    [Fact]
    public void SearchByName_NoMatch_ReturnsEmpty()
    {
        Assert.Empty(_employees.SearchByName("zzz"));
    }

    // Q4
    [Fact]
    public void SortedBySalaryDescending_TiedSalaries_BreaksTieByName()
    {
        var result = _employees.SortedBySalaryDescending();

        // Bob Jones and Dan Brown both earn 80k -> alphabetical: Bob then Dan.
        Assert.Equal([1, 2, 4, 3, 5], Ids(result));
    }

    // Q5
    [Fact]
    public void TopEarners_Two_ReturnsHighestTwo()
    {
        Assert.Equal([1, 2], Ids(_employees.TopEarners(2)));
    }

    [Fact]
    public void TopEarners_MoreThanAvailable_ReturnsAll()
    {
        Assert.Equal(5, _employees.TopEarners(100).Count());
    }

    [Fact]
    public void TopEarners_Zero_ReturnsEmpty()
    {
        Assert.Empty(_employees.TopEarners(0));
    }

    [Fact]
    public void TopEarners_Negative_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _employees.TopEarners(-1));
    }

    // Q6
    [Fact]
    public void HiredOnOrAfter_Date_IncludesSameDayAndOrdersByHireDate()
    {
        var result = _employees.HiredOnOrAfter(new DateOnly(2023, 3, 1));

        Assert.Equal([4, 3, 5], Ids(result));   // 2023-03-01, 2024-01-01, 2025-05-20
    }

    // Q7
    [Fact]
    public void SummarizeByDepartment_ComputesHeadcountTotalAndAverage()
    {
        var summaries = _employees.SummarizeByDepartment(_departments);

        var engineering = Assert.Single(summaries, s => s.DepartmentId == 1);
        Assert.Equal(3, engineering.Headcount);
        Assert.Equal(260_000m, engineering.TotalSalary);
        Assert.Equal(86_666.67m, engineering.AverageSalary);

        var sales = Assert.Single(summaries, s => s.DepartmentId == 2);
        Assert.Equal(2, sales.Headcount);
        Assert.Equal(62_500m, sales.AverageSalary);
    }

    [Fact]
    public void SummarizeByDepartment_DepartmentWithoutEmployees_IncludedWithZeros()
    {
        var summaries = _employees.SummarizeByDepartment(_departments);

        var finance = Assert.Single(summaries, s => s.DepartmentId == 3);
        Assert.Equal(0, finance.Headcount);
        Assert.Equal(0m, finance.AverageSalary);
        Assert.Equal("Finance", summaries[^1].DepartmentName);   // ordered by headcount desc
    }

    [Fact]
    public void SummarizeByDepartment_NoEmployees_AllZeroHeadcount()
    {
        var summaries = new List<Employee>().SummarizeByDepartment(_departments);

        Assert.All(summaries, s => Assert.Equal(0, s.Headcount));
    }

    // Q8
    [Fact]
    public void WithDepartmentNames_JoinsNames()
    {
        var details = _employees.WithDepartmentNames(_departments).ToList();

        Assert.Equal(5, details.Count);
        Assert.Equal("Sales", details.Single(d => d.Id == 5).DepartmentName);
    }

    [Fact]
    public void WithDepartmentNames_UnknownDepartment_IsExcluded()
    {
        var employees = new List<Employee> { TestData.FullTime(id: 1, departmentId: 99) };

        Assert.Empty(employees.WithDepartmentNames(_departments));
    }

    // Q9
    [Fact]
    public void HeadcountByType_CountsEachType()
    {
        var counts = _employees.HeadcountByType();

        Assert.Equal(3, counts[EmploymentType.FullTime]);
        Assert.Equal(2, counts[EmploymentType.Contract]);
    }

    // Q10
    [Fact]
    public void MissingEmail_ReturnsEmployeesWithoutEmail()
    {
        Assert.Equal([4], Ids(_employees.MissingEmail()));
    }

    // Q11
    [Theory]
    [InlineData(1, 2, new[] { 1, 2 })]
    [InlineData(3, 2, new[] { 5 })]          // last, partial page
    [InlineData(4, 2, new int[0])]           // past the end
    public void Page_ReturnsRequestedSlice(int page, int size, int[] expected)
    {
        Assert.Equal(expected, Ids(_employees.Page(page, size)));
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    public void Page_InvalidArguments_Throws(int page, int size)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _employees.Page(page, size));
    }

    // Q12
    [Fact]
    public void TotalAnnualCost_UsesEachTypesOwnCalculation()
    {
        // FT: (120k + 80k + 80k) * 1.2 = 336k; Contract: (60k + 45k) * 1.1 = 115.5k
        Assert.Equal(451_500m, _employees.TotalAnnualCost());
    }

    // Q13
    [Fact]
    public void ContractsEndingOnOrBefore_ExcludesOpenEndedAndFullTime()
    {
        var result = _employees.ContractsEndingOnOrBefore(new DateOnly(2026, 6, 30));

        Assert.Equal([3], result.Select(c => c.Id));
    }

    // Deferred execution
    [Fact]
    public void InDepartment_ItemAddedAfterQueryDefined_IsIncluded()
    {
        // Arrange: define the query, but do not enumerate it yet.
        var engineers = _employees.InDepartment(1);

        // Act: change the source afterwards.
        _employees.Add(TestData.FullTime(id: 6, departmentId: 1));

        // Assert: the query runs now, so it sees the new employee.
        Assert.Equal(4, engineers.Count());
    }

    [Fact]
    public void InDepartment_MaterializedWithToList_DoesNotSeeLaterChanges()
    {
        var snapshot = _employees.InDepartment(1).ToList();

        _employees.Add(TestData.FullTime(id: 6, departmentId: 1));

        Assert.Equal(3, snapshot.Count);
    }
}
