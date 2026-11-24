using EmployeeManagement.Application.Common;
using EmployeeManagement.Application.Dtos;
using EmployeeManagement.Application.Services;
using EmployeeManagement.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace EmployeeManagement.Api.Tests.Unit;

public class DepartmentServiceTests
{
    private readonly FakeEmployeeRepository _employees;
    private readonly FakeDepartmentRepository _departments;
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly DepartmentService _service;

    public DepartmentServiceTests()
    {
        (_employees, _departments) = Seed.Create();
        _service = new DepartmentService(_departments, _employees, _unitOfWork, NullLogger<DepartmentService>.Instance);
    }

    [Fact]
    public async Task ListAsync_ReturnsHeadcountPerDepartment()
    {
        var result = await _service.ListAsync();

        Assert.Equal(3, result.Single(d => d.Name == "Engineering").Headcount);
        Assert.Equal(0, result.Single(d => d.Name == "Finance").Headcount);
    }

    [Fact]
    public async Task GetAsync_Unknown_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAsync(99));
    }

    [Fact]
    public async Task CreateAsync_NewName_AddsTrimmedAndSaves()
    {
        var result = await _service.CreateAsync(new CreateDepartmentRequest { Name = "  Legal " });

        Assert.Equal("Legal", result.Name);
        Assert.Equal(0, result.Headcount);
        Assert.Equal(1, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task CreateAsync_DuplicateNameDifferentCase_ThrowsConflict()
    {
        await Assert.ThrowsAsync<ConflictException>(() => _service.CreateAsync(new CreateDepartmentRequest { Name = "sales" }));
        Assert.Equal(0, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task DeleteAsync_EmptyDepartment_Removes()
    {
        await _service.DeleteAsync(3);

        Assert.DoesNotContain(_departments.Items, d => d.Id == 3);
    }

    [Fact]
    public async Task DeleteAsync_DepartmentWithEmployees_ThrowsConflictAndKeepsIt()
    {
        var ex = await Assert.ThrowsAsync<ConflictException>(() => _service.DeleteAsync(1));

        Assert.Contains("3 employee(s)", ex.Message);
        Assert.Contains(_departments.Items, d => d.Id == 1);
    }

    [Fact]
    public async Task GetSummaryAsync_ReusesWeek1Query()
    {
        var summary = await _service.GetSummaryAsync();

        var engineering = summary.Single(s => s.DepartmentName == "Engineering");
        Assert.Equal(260_000m, engineering.TotalSalary);
        Assert.Equal(86_666.67m, engineering.AverageSalary);
    }
}

public class AuthServiceTests
{
    private readonly FakeUserRepository _users = new();
    private readonly FakePasswordHasher _hasher = new();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        var hr = new AppUser(2, "hr", Roles.HR);
        hr.SetPasswordHash(_hasher.Hash(hr, "correct-password"));
        _users.Items.Add(hr);
        _service = new AuthService(_users, _hasher, new FakeTokenService(), NullLogger<AuthService>.Instance);
    }

    [Fact]
    public async Task LoginAsync_CorrectPassword_ReturnsBearerToken()
    {
        var result = await _service.LoginAsync(new LoginRequest { UserName = "hr", Password = "correct-password" });

        Assert.NotNull(result);
        Assert.Equal("token-for-hr", result.AccessToken);
        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal(Roles.HR, result.Role);
        Assert.Equal(FakeTokenService.ExpiresAt, result.ExpiresAt);
    }

    [Fact]
    public async Task LoginAsync_UserNameDifferentCaseAndSpaces_StillMatches()
    {
        var result = await _service.LoginAsync(new LoginRequest { UserName = "  HR ", Password = "correct-password" });

        Assert.NotNull(result);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsNull()
    {
        Assert.Null(await _service.LoginAsync(new LoginRequest { UserName = "hr", Password = "wrong" }));
    }

    [Fact]
    public async Task LoginAsync_UnknownUser_ReturnsNullButStillVerifiesAHash()
    {
        // Running a hash check even for unknown users keeps response times similar,
        // so attackers can't discover which user names exist.
        var result = await _service.LoginAsync(new LoginRequest { UserName = "nobody", Password = "x" });

        Assert.Null(result);
        Assert.Equal(1, _hasher.VerifyCalls);
    }
}
