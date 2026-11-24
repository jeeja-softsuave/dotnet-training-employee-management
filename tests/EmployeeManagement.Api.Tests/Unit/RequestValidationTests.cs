using System.ComponentModel.DataAnnotations;
using EmployeeManagement.Application.Dtos;
using EmployeeManagement.Core.Models;

namespace EmployeeManagement.Api.Tests.Unit;

/// <summary>
/// Runs the same DataAnnotations + IValidatableObject checks that [ApiController] runs before an
/// action executes. Returns the names of the fields with errors.
/// </summary>
internal static class Validate
{
    public static string[] ErrorFields(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results.SelectMany(r => r.MemberNames).Distinct().Order().ToArray();
    }
}

public class CreateEmployeeRequestValidationTests
{
    private static CreateEmployeeRequest Valid() => new()
    {
        Name = "Kiran Shah",
        Email = "kiran@example.com",
        Type = EmploymentType.FullTime,
        Salary = 70_000m,
        HireDate = new DateOnly(2026, 11, 1),
        DepartmentId = 1,
    };

    [Fact]
    public void Valid_HasNoErrors()
    {
        Assert.Empty(Validate.ErrorFields(Valid()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]       // below minimum length 2
    public void Name_TooShort_IsInvalid(string name)
    {
        var request = Valid();
        request.Name = name;

        Assert.Contains("Name", Validate.ErrorFields(request));
    }

    [Fact]
    public void Name_Whitespace_IsInvalid()
    {
        var request = Valid();
        request.Name = "     ";

        Assert.Contains("Name", Validate.ErrorFields(request));
    }

    [Fact]
    public void Name_TooLong_IsInvalid()
    {
        var request = Valid();
        request.Name = new string('a', Employee.MaxNameLength + 1);

        Assert.Contains("Name", Validate.ErrorFields(request));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("a@")]
    public void Email_BadFormat_IsInvalid(string email)
    {
        var request = Valid();
        request.Email = email;

        Assert.Contains("Email", Validate.ErrorFields(request));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10_000_000.01)]
    public void Salary_OutOfRange_IsInvalid(decimal salary)
    {
        var request = Valid();
        request.Salary = salary;

        Assert.Contains("Salary", Validate.ErrorFields(request));
    }

    [Fact]
    public void Salary_Boundaries_AreValid()
    {
        var low = Valid();
        low.Salary = 0.01m;
        var high = Valid();
        high.Salary = Employee.MaxSalary;

        Assert.Empty(Validate.ErrorFields(low));
        Assert.Empty(Validate.ErrorFields(high));
    }

    [Fact]
    public void MissingTypeHireDateAndDepartment_AreInvalid()
    {
        var request = Valid();
        request.Type = null;
        request.HireDate = null;
        request.DepartmentId = 0;

        Assert.Equal(["DepartmentId", "HireDate", "Type"], Validate.ErrorFields(request));
    }

    [Fact]
    public void ContractEndDate_OnFullTimeEmployee_IsInvalid()
    {
        var request = Valid();
        request.ContractEndDate = new DateOnly(2027, 1, 1);

        Assert.Equal(["ContractEndDate"], Validate.ErrorFields(request));
    }

    [Theory]
    [InlineData(0)]     // same day as hire
    [InlineData(-1)]    // before hire
    public void ContractEndDate_NotAfterHireDate_IsInvalid(int daysFromHire)
    {
        var request = Valid();
        request.Type = EmploymentType.Contract;
        request.ContractEndDate = request.HireDate!.Value.AddDays(daysFromHire);

        Assert.Equal(["ContractEndDate"], Validate.ErrorFields(request));
    }

    [Fact]
    public void ContractEndDate_DayAfterHire_IsValid()
    {
        var request = Valid();
        request.Type = EmploymentType.Contract;
        request.ContractEndDate = request.HireDate!.Value.AddDays(1);

        Assert.Empty(Validate.ErrorFields(request));
    }
}

public class OtherRequestValidationTests
{
    [Fact]
    public void Patch_NoFields_IsInvalid()
    {
        Assert.NotEmpty(Validate.ErrorFields(new PatchEmployeeRequest()));
    }

    [Fact]
    public void Patch_OneField_IsValid()
    {
        Assert.Empty(Validate.ErrorFields(new PatchEmployeeRequest { Salary = 1_000m }));
    }

    [Fact]
    public void Patch_BlankName_IsInvalid()
    {
        Assert.Contains("Name", Validate.ErrorFields(new PatchEmployeeRequest { Name = "   " }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100.01)]
    public void Raise_PercentageOutOfRange_IsInvalid(decimal percentage)
    {
        Assert.Contains("Percentage", Validate.ErrorFields(new RaiseRequest { Percentage = percentage }));
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(100)]
    public void Raise_PercentageBoundaries_AreValid(decimal percentage)
    {
        Assert.Empty(Validate.ErrorFields(new RaiseRequest { Percentage = percentage }));
    }

    [Fact]
    public void Update_MissingName_IsInvalid()
    {
        Assert.Contains("Name", Validate.ErrorFields(new UpdateEmployeeRequest { Salary = 1m, DepartmentId = 1 }));
    }

    [Fact]
    public void Login_MissingFields_AreInvalid()
    {
        Assert.Equal(["Password", "UserName"], Validate.ErrorFields(new LoginRequest()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("X")]
    public void CreateDepartment_NameTooShort_IsInvalid(string name)
    {
        Assert.Contains("Name", Validate.ErrorFields(new CreateDepartmentRequest { Name = name }));
    }
}

public class EmployeeQueryParametersValidationTests
{
    [Fact]
    public void Defaults_AreValid()
    {
        var query = new EmployeeQueryParameters();

        Assert.Empty(Validate.ErrorFields(query));
        Assert.Equal(1, query.Page);
        Assert.Equal(EmployeeQueryParameters.DefaultPageSize, query.PageSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void PageSize_OutOfRange_IsInvalid(int pageSize)
    {
        Assert.Contains("PageSize", Validate.ErrorFields(new EmployeeQueryParameters { PageSize = pageSize }));
    }

    [Fact]
    public void Page_Zero_IsInvalid()
    {
        Assert.Contains("Page", Validate.ErrorFields(new EmployeeQueryParameters { Page = 0 }));
    }

    [Fact]
    public void MinSalaryAboveMax_IsInvalid()
    {
        var query = new EmployeeQueryParameters { MinSalary = 90_000m, MaxSalary = 80_000m };

        Assert.Equal(["MaxSalary", "MinSalary"], Validate.ErrorFields(query));
    }

    [Fact]
    public void MinSalaryEqualToMax_IsValid()
    {
        Assert.Empty(Validate.ErrorFields(new EmployeeQueryParameters { MinSalary = 80_000m, MaxSalary = 80_000m }));
    }

    [Theory]
    [InlineData("email")]
    [InlineData("salary; DROP TABLE Employees")]
    public void SortBy_NotAllowed_IsInvalid(string sortBy)
    {
        Assert.Contains("SortBy", Validate.ErrorFields(new EmployeeQueryParameters { SortBy = sortBy }));
    }

    [Theory]
    [InlineData("NAME")]
    [InlineData("hiredate")]
    public void SortBy_IsCaseInsensitive(string sortBy)
    {
        Assert.Empty(Validate.ErrorFields(new EmployeeQueryParameters { SortBy = sortBy }));
    }

    [Fact]
    public void SortDirection_Unknown_IsInvalid()
    {
        Assert.Contains("SortDirection", Validate.ErrorFields(new EmployeeQueryParameters { SortDirection = "up" }));
    }
}
