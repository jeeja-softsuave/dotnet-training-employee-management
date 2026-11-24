using EmployeeManagement.Application.Common;
using EmployeeManagement.Application.Dtos;
using EmployeeManagement.Application.Services;
using EmployeeManagement.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace EmployeeManagement.Api.Tests.Unit;

public class EmployeeServiceTests
{
    private readonly FakeEmployeeRepository _employees;
    private readonly FakeDepartmentRepository _departments;
    private readonly FakeUnitOfWork _unitOfWork = new();

    public EmployeeServiceTests() => (_employees, _departments) = Seed.Create();

    // Dependencies are injected through the constructor, so each test picks the user's roles.
    private EmployeeService CreateService(params string[] roles) =>
        new(_employees, _departments, _unitOfWork, new FakeCurrentUser(roles), NullLogger<EmployeeService>.Instance);

    private static CreateEmployeeRequest ValidCreate() => new()
    {
        Name = "Kiran Shah",
        Email = "  Kiran.Shah@Example.com ",
        Type = EmploymentType.FullTime,
        Salary = 70_000m,
        HireDate = new DateOnly(2026, 11, 1),
        DepartmentId = 2,
    };

    // ---------- Get / Search ----------

    [Fact]
    public async Task GetAsync_ExistingId_ReturnsMappedResponse()
    {
        var result = await CreateService().GetAsync(3);

        Assert.Equal("Carol White", result.Name);
        Assert.Equal(EmploymentType.Contract, result.Type);
        Assert.Equal(new DateOnly(2026, 6, 30), result.ContractEndDate);
        Assert.Equal(66_000m, result.AnnualCost);   // 60k + 10% agency fee (Week 1 polymorphism)
        Assert.Equal("B", result.SalaryBand);
    }

    [Fact]
    public async Task GetAsync_UnknownId_ThrowsNotFound()
    {
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => CreateService().GetAsync(99));

        Assert.Equal(99, ex.Key);
    }

    [Fact]
    public async Task SearchAsync_PageTwoOfSize2_ReturnsSliceAndTotals()
    {
        var result = await CreateService().SearchAsync(new EmployeeQueryParameters { Page = 2, PageSize = 2 });

        Assert.Equal([3, 4], result.Items.Select(e => e.Id));
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.True(result.HasPreviousPage);
    }

    // ---------- Create ----------

    [Fact]
    public async Task CreateAsync_Valid_AddsSavesAndNormalizesEmail()
    {
        // Act
        var result = await CreateService(Roles.HR).CreateAsync(ValidCreate());

        // Assert
        Assert.Equal("Kiran Shah", result.Name);
        Assert.Equal("kiran.shah@example.com", result.Email);
        Assert.Equal("Sales", result.DepartmentName);
        Assert.Equal(6, _employees.Items.Count);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateAsync_Contract_CreatesContractEmployee()
    {
        var request = ValidCreate();
        request.Type = EmploymentType.Contract;
        request.ContractEndDate = new DateOnly(2027, 11, 1);

        var result = await CreateService(Roles.HR).CreateAsync(request);

        Assert.Equal(EmploymentType.Contract, result.Type);
        Assert.IsType<ContractEmployee>(_employees.Items[^1]);
    }

    [Fact]
    public async Task CreateAsync_UnknownDepartment_ThrowsBusinessRuleAndDoesNotSave()
    {
        var request = ValidCreate();
        request.DepartmentId = 42;

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => CreateService(Roles.HR).CreateAsync(request));

        Assert.Contains("DepartmentId", ex.Errors.Keys);
        Assert.Equal(0, _unitOfWork.SaveCount);
        Assert.Equal(5, _employees.Items.Count);
    }

    [Fact]
    public async Task CreateAsync_DuplicateEmailDifferentCase_ThrowsConflict()
    {
        var request = ValidCreate();
        request.Email = "ALICE@example.com";

        await Assert.ThrowsAsync<ConflictException>(() => CreateService(Roles.HR).CreateAsync(request));
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateAsync_NoEmail_IsAllowed()
    {
        var request = ValidCreate();
        request.Email = "   ";

        var result = await CreateService(Roles.HR).CreateAsync(request);

        Assert.Null(result.Email);
    }

    // ---------- Update (PUT) / Patch ----------

    [Fact]
    public async Task UpdateAsync_ReplacesAllEditableFields()
    {
        var request = new UpdateEmployeeRequest { Name = "Robert Jones", Email = null, Salary = 85_000m, DepartmentId = 2 };

        await CreateService(Roles.HR).UpdateAsync(2, request);

        var bob = _employees.Items.Single(e => e.Id == 2);
        Assert.Equal("Robert Jones", bob.Name);
        Assert.Null(bob.Email);             // PUT: a missing email means "remove it"
        Assert.Equal(85_000m, bob.Salary);
        Assert.Equal(2, bob.DepartmentId);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task UpdateAsync_KeepingOwnEmail_IsNotAConflict()
    {
        var request = new UpdateEmployeeRequest { Name = "Bob Jones", Email = "bob@example.com", Salary = 80_000m, DepartmentId = 1 };

        await CreateService(Roles.HR).UpdateAsync(2, request);

        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task UpdateAsync_UnknownEmployee_ThrowsNotFound()
    {
        var request = new UpdateEmployeeRequest { Name = "X Y", Salary = 1m, DepartmentId = 1 };

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService(Roles.HR).UpdateAsync(99, request));
    }

    [Fact]
    public async Task PatchAsync_OnlySalary_LeavesOtherFieldsUnchanged()
    {
        var result = await CreateService(Roles.HR).PatchAsync(2, new PatchEmployeeRequest { Salary = 90_000m });

        Assert.Equal(90_000m, result.Salary);
        Assert.Equal("Bob Jones", result.Name);
        Assert.Equal("bob@example.com", result.Email);   // PATCH: a missing email means "don't change"
        Assert.Equal(1, result.DepartmentId);
    }

    [Fact]
    public async Task PatchAsync_MoveToUnknownDepartment_ThrowsBusinessRule()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            CreateService(Roles.HR).PatchAsync(2, new PatchEmployeeRequest { DepartmentId = 42 }));
        Assert.Equal(1, _employees.Items.Single(e => e.Id == 2).DepartmentId);
    }

    [Fact]
    public async Task PatchAsync_EmailUsedByAnother_ThrowsConflict()
    {
        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateService(Roles.HR).PatchAsync(2, new PatchEmployeeRequest { Email = "alice@example.com" }));
    }

    [Fact]
    public async Task PatchAsync_InvalidSalaryForDomain_ThrowsArgumentException()
    {
        // The DTO allows this, but the domain entity still enforces its own rule (defence in depth).
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            CreateService(Roles.HR).PatchAsync(2, new PatchEmployeeRequest { Salary = -5m }));
    }

    // ---------- Delete ----------

    [Fact]
    public async Task DeleteAsync_Existing_RemovesAndSaves()
    {
        await CreateService(Roles.Admin).DeleteAsync(5);

        Assert.DoesNotContain(_employees.Items, e => e.Id == 5);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task DeleteAsync_Unknown_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => CreateService(Roles.Admin).DeleteAsync(99));
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    // ---------- Raise (authorization-related business rule) ----------

    [Theory]
    [InlineData(Roles.HR, 20)]      // boundary: exactly the HR limit
    [InlineData(Roles.Admin, 50)]
    [InlineData(Roles.Admin, 100)]
    public async Task GiveRaiseAsync_WithinRoleLimit_UpdatesSalary(string role, decimal percentage)
    {
        var result = await CreateService(role).GiveRaiseAsync(2, new RaiseRequest { Percentage = percentage });

        Assert.Equal(Math.Round(80_000m * (1 + percentage / 100m), 2), result.Salary);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Theory]
    [InlineData(Roles.HR, 20.01)]   // just above the HR limit
    [InlineData(Roles.HR, 30)]
    [InlineData(Roles.Viewer, 1)]
    public async Task GiveRaiseAsync_AboveRoleLimit_ThrowsForbiddenAndLeavesSalary(string role, decimal percentage)
    {
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            CreateService(role).GiveRaiseAsync(2, new RaiseRequest { Percentage = percentage }));

        Assert.Equal(80_000m, _employees.Items.Single(e => e.Id == 2).Salary);
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task GiveRaiseAsync_UnknownEmployee_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateService(Roles.Admin).GiveRaiseAsync(99, new RaiseRequest { Percentage = 5 }));
    }
}
