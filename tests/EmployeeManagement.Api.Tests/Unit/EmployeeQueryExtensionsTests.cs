using EmployeeManagement.Application.Dtos;
using EmployeeManagement.Application.Queries;
using EmployeeManagement.Core.Models;

namespace EmployeeManagement.Api.Tests.Unit;

/// <summary>
/// The filtering/sorting/paging rules tested without a database: the same IQueryable code that EF
/// Core turns into SQL runs here on an in-memory list.
/// </summary>
public class EmployeeQueryExtensionsTests
{
    private readonly IQueryable<Employee> _source = Seed.Create().Employees.Items.AsQueryable();

    private int[] Run(EmployeeQueryParameters q) =>
        _source.ApplyFilters(q).ApplySorting(q).ApplyPaging(q).Select(e => e.Id).ToArray();

    [Fact]
    public void NoFilters_ReturnsAllOrderedById()
    {
        Assert.Equal([1, 2, 3, 4, 5], Run(new EmployeeQueryParameters()));
    }

    [Fact]
    public void DepartmentFilter_ReturnsOnlyThatDepartment()
    {
        Assert.Equal([4, 5], Run(new EmployeeQueryParameters { DepartmentId = 2 }));
    }

    [Theory]
    [InlineData(EmploymentType.FullTime, new[] { 1, 2, 4 })]
    [InlineData(EmploymentType.Contract, new[] { 3, 5 })]
    public void TypeFilter_UsesClassHierarchy(EmploymentType type, int[] expected)
    {
        Assert.Equal(expected, Run(new EmployeeQueryParameters { Type = type }));
    }

    [Fact]
    public void SalaryRange_IncludesBothBounds()
    {
        // 60k (Carol) and 80k (Bob, Dan) are exactly on the bounds: all included (DEF-001 rule).
        Assert.Equal([2, 3, 4], Run(new EmployeeQueryParameters { MinSalary = 60_000m, MaxSalary = 80_000m }));
    }

    [Fact]
    public void MinSalaryOnly_ReturnsAtOrAbove()
    {
        Assert.Equal([1], Run(new EmployeeQueryParameters { MinSalary = 100_000m }));
    }

    [Theory]
    [InlineData("SMITH", new[] { 1 })]          // name, case-insensitive
    [InlineData("  eve@ ", new[] { 5 })]        // email, trimmed
    [InlineData("example.com", new[] { 1, 2, 3, 5 })]   // email match; Dan (4) has no email
    [InlineData("nobody", new int[0])]
    public void Search_MatchesNameOrEmail(string term, int[] expected)
    {
        Assert.Equal(expected, Run(new EmployeeQueryParameters { Search = term }));
    }

    [Fact]
    public void Search_DoesNotFailOnEmployeeWithoutEmail()
    {
        // Dan has no email; the null check must come before ToLower().
        Assert.Equal([4], Run(new EmployeeQueryParameters { Search = "dan" }));
    }

    [Fact]
    public void CombinedFilters_AreAnded()
    {
        var q = new EmployeeQueryParameters { DepartmentId = 1, Type = EmploymentType.FullTime, MaxSalary = 100_000m };

        Assert.Equal([2], Run(q));
    }

    [Fact]
    public void SortBySalaryDesc_TiesBrokenByIdDesc()
    {
        // Bob (2) and Dan (4) both earn 80k.
        Assert.Equal([1, 4, 2, 3, 5], Run(new EmployeeQueryParameters { SortBy = "salary", SortDirection = "desc" }));
    }

    [Fact]
    public void SortBySalaryAsc_TiesBrokenByIdAsc()
    {
        Assert.Equal([5, 3, 2, 4, 1], Run(new EmployeeQueryParameters { SortBy = "Salary" }));
    }

    [Fact]
    public void SortByName_IsAlphabetical()
    {
        Assert.Equal([1, 2, 3, 4, 5], Run(new EmployeeQueryParameters { SortBy = "name" }));
        Assert.Equal([5, 4, 3, 2, 1], Run(new EmployeeQueryParameters { SortBy = "name", SortDirection = "DESC" }));
    }

    [Fact]
    public void SortByHireDate_OldestFirst()
    {
        Assert.Equal([1, 2, 4, 3, 5], Run(new EmployeeQueryParameters { SortBy = "hireDate" }));
    }

    [Theory]
    [InlineData(1, 2, new[] { 1, 2 })]
    [InlineData(3, 2, new[] { 5 })]          // last, partial page
    [InlineData(4, 2, new int[0])]           // past the end
    public void Paging_ReturnsRequestedSlice(int page, int pageSize, int[] expected)
    {
        Assert.Equal(expected, Run(new EmployeeQueryParameters { Page = page, PageSize = pageSize }));
    }

    [Fact]
    public void Paging_AppliedAfterFilterAndSort()
    {
        var q = new EmployeeQueryParameters { DepartmentId = 1, SortBy = "salary", SortDirection = "desc", Page = 2, PageSize = 1 };

        Assert.Equal([2], Run(q));   // engineering by salary desc: Alice, Bob, Carol -> page 2 = Bob
    }

    [Fact]
    public void ApplyPaging_InvalidPage_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _source.ApplyPaging(new EmployeeQueryParameters { Page = 0 }));
    }
}
