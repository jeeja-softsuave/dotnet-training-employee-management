using EmployeeManagement.Core.Models;

namespace EmployeeManagement.Tests.Models;

public class DepartmentTests
{
    [Fact]
    public void Equals_SameValues_AreEqual()
    {
        // Records compare by value, not by reference.
        var a = new Department(1, "Engineering");
        var b = new Department(1, "Engineering");

        Assert.Equal(a, b);
        Assert.False(ReferenceEquals(a, b));
    }

    [Fact]
    public void Constructor_NameWithSpaces_IsTrimmed()
    {
        var department = new Department(1, "  Sales ");

        Assert.Equal("Sales", department.Name);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NonPositiveId_Throws(int id)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Department(id, "Sales"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Constructor_BlankName_Throws(string name)
    {
        Assert.Throws<ArgumentException>(() => new Department(1, name));
    }
}

public class SalaryRangeTests
{
    [Fact]
    public void Contains_ValueInside_ReturnsTrue()
    {
        var range = new SalaryRange(10_000m, 20_000m);

        Assert.True(range.Contains(15_000m));
    }

    [Theory]
    [InlineData(9_999.99)]
    [InlineData(20_000.01)]
    public void Contains_ValueJustOutside_ReturnsFalse(decimal salary)
    {
        var range = new SalaryRange(10_000m, 20_000m);

        Assert.False(range.Contains(salary));
    }

    [Fact]
    public void Contains_ValueEqualToMin_ReturnsTrue()
    {
        var range = new SalaryRange(10_000m, 20_000m);

        Assert.True(range.Contains(10_000m));
    }

    // Regression test for defect DEF-001: the upper bound was exclusive (salary < Max),
    // so filtering 50,000-80,000 left out employees earning exactly 80,000.
    [Fact]
    public void Contains_ValueEqualToMax_ReturnsTrue()
    {
        var range = new SalaryRange(10_000m, 20_000m);

        Assert.True(range.Contains(20_000m));
    }

    // Regression test for DEF-001: a single-value range (Min == Max) must match that value.
    [Fact]
    public void Contains_MinEqualsMax_MatchesExactValue()
    {
        var range = new SalaryRange(80_000m, 80_000m);

        Assert.True(range.Contains(80_000m));
    }

    [Fact]
    public void Copy_IsIndependentValue()
    {
        // Struct = value type: assignment copies the value. Records structs also compare by value.
        var a = new SalaryRange(1m, 2m);
        var b = a;

        Assert.Equal(a, b);
    }

    [Fact]
    public void Constructor_NegativeMin_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SalaryRange(-1m, 10m));
    }

    [Fact]
    public void Constructor_MaxLessThanMin_Throws()
    {
        Assert.Throws<ArgumentException>(() => new SalaryRange(10m, 9.99m));
    }
}
