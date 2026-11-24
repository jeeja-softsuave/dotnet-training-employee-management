using System.Net;
using System.Net.Http.Json;
using EmployeeManagement.Application.Common;
using EmployeeManagement.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Api.Tests.Integration;

/// <summary>Read-only queries against the seeded data (10 employees, 4 departments).</summary>
public class EmployeeQueryEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private async Task<PagedResult<EmployeeResponse>> GetPageAsync(string query)
    {
        var client = await factory.CreateClientForAsync("viewer");
        var response = await client.GetAsync("/api/employees" + query);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PagedResult<EmployeeResponse>>(TestJson.Options))!;
    }

    [Fact]
    public async Task List_Defaults_FirstPageOfTen()
    {
        var page = await GetPageAsync("");

        Assert.Equal(10, page.TotalCount);
        Assert.Equal(10, page.Items.Count);
        Assert.Equal(1, page.Items[0].Id);
        Assert.Equal("Engineering", page.Items[0].DepartmentName);   // loaded via Include (JOIN)
    }

    [Fact]
    public async Task List_PagingAndSortingRunInDatabase()
    {
        var page = await GetPageAsync("?sortBy=salary&sortDirection=desc&page=1&pageSize=3");

        Assert.Equal(["Vikram Rao", "Asha Menon", "Arjun Das"], page.Items.Select(e => e.Name));
        Assert.Equal(10, page.TotalCount);
        Assert.Equal(4, page.TotalPages);
    }

    [Fact]
    public async Task List_FilterByDepartmentAndType()
    {
        var page = await GetPageAsync("?departmentId=1&type=Contract");

        Assert.Equal([3, 10], page.Items.Select(e => e.Id));
    }

    [Fact]
    public async Task List_SalaryRangeInclusive_InSql()
    {
        var page = await GetPageAsync("?minSalary=60000&maxSalary=72000&sortBy=salary");

        Assert.Equal([60_000m, 66_000m, 72_000m], page.Items.Select(e => e.Salary));
    }

    [Fact]
    public async Task List_SearchIsCaseInsensitive()
    {
        var page = await GetPageAsync("?search=KUMAR");

        Assert.Equal("Ravi Kumar", Assert.Single(page.Items).Name);
    }

    [Theory]
    [InlineData("?pageSize=500", "PageSize")]
    [InlineData("?sortBy=password", "SortBy")]
    [InlineData("?minSalary=9&maxSalary=1", "MinSalary")]
    public async Task List_InvalidQuery_Returns400WithFieldError(string query, string field)
    {
        var client = await factory.CreateClientForAsync("viewer");

        var response = await client.GetAsync("/api/employees" + query);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains(field, problem!.Errors.Keys);
    }

    [Fact]
    public async Task GetById_Unknown_Returns404ProblemWithCorrelationId()
    {
        var client = await factory.CreateClientForAsync("viewer");
        client.DefaultRequestHeaders.Add("X-Correlation-Id", "test-404");

        var response = await client.GetAsync("/api/employees/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("test-404", response.Headers.GetValues("X-Correlation-Id").Single());
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"correlationId\":\"test-404\"", body);
        Assert.Contains("\"traceId\"", body);
    }

    [Fact]
    public async Task DepartmentSummary_ReturnsTotals()
    {
        var client = await factory.CreateClientForAsync("viewer");

        var summary = await client.GetFromJsonAsync<List<Core.Models.DepartmentSummary>>("/api/departments/summary", TestJson.Options);

        var engineering = summary!.Single(s => s.DepartmentName == "Engineering");
        Assert.Equal(5, engineering.Headcount);
        Assert.Equal(439_000m, engineering.TotalSalary);
    }

    [Fact]
    public async Task OpenApiDocument_IsServedAndDescribesBearerAuth()
    {
        var json = await factory.CreateClient().GetStringAsync("/openapi/v1.json");

        Assert.Contains("\"/api/employees/{id}\"", json);
        Assert.Contains("\"bearerFormat\": \"JWT\"", json);
    }
}

/// <summary>Create / update / patch / delete with real persistence.</summary>
public class EmployeeCommandEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static object NewEmployee(string name, string? email = null, int departmentId = 2) => new
    {
        name,
        email,
        type = "FullTime",
        salary = 70_000,
        hireDate = "2026-11-01",
        departmentId,
    };

    private async Task<EmployeeResponse> CreateAsync(HttpClient client, string name, string? email = null)
    {
        var response = await client.PostAsJsonAsync("/api/employees", NewEmployee(name, email));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<EmployeeResponse>(TestJson.Options))!;
    }

    [Fact]
    public async Task Create_Returns201WithLocationAndPersists()
    {
        var client = await factory.CreateClientForAsync("hr");

        var response = await client.PostAsJsonAsync("/api/employees", NewEmployee("Nisha Paul", "Nisha.Paul@Example.com"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<EmployeeResponse>(TestJson.Options);
        Assert.True(created!.Id > 10);                                  // id generated by the database
        Assert.Equal("nisha.paul@example.com", created.Email);
        Assert.Equal("Human Resources", created.DepartmentName);
        Assert.Equal($"/api/employees/{created.Id}", response.Headers.Location!.AbsolutePath);

        var fetched = await client.GetFromJsonAsync<EmployeeResponse>(response.Headers.Location, TestJson.Options);
        Assert.Equal("Nisha Paul", fetched!.Name);
    }

    [Fact]
    public async Task Create_Contract_StoresEndDate()
    {
        var client = await factory.CreateClientForAsync("hr");

        var response = await client.PostAsJsonAsync("/api/employees", new
        {
            name = "Omar Farooq", type = "Contract", salary = 40_000, hireDate = "2026-01-01",
            departmentId = 3, contractEndDate = "2026-12-31",
        });

        var created = await response.Content.ReadFromJsonAsync<EmployeeResponse>(TestJson.Options);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(new DateOnly(2026, 12, 31), created!.ContractEndDate);
        Assert.Equal(44_000m, created.AnnualCost);
    }

    [Fact]
    public async Task Create_InvalidBody_Returns400WithAllFieldErrors()
    {
        var client = await factory.CreateClientForAsync("hr");

        var response = await client.PostAsJsonAsync("/api/employees", new { name = "", salary = 0, departmentId = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Superset(new HashSet<string> { "Name", "Salary", "DepartmentId", "Type", "HireDate" }, problem!.Errors.Keys.ToHashSet());
    }

    [Fact]
    public async Task Create_MalformedJson_Returns400()
    {
        var client = await factory.CreateClientForAsync("hr");

        var response = await client.PostAsync("/api/employees",
            new StringContent("{ \"name\": ", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_UnknownDepartment_Returns400WithDepartmentError()
    {
        var client = await factory.CreateClientForAsync("hr");

        var response = await client.PostAsJsonAsync("/api/employees", NewEmployee("Lost Person", departmentId: 99));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("DepartmentId", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Create_DuplicateEmail_Returns409()
    {
        var client = await factory.CreateClientForAsync("hr");

        var response = await client.PostAsJsonAsync("/api/employees", NewEmployee("Asha Twin", "ASHA.MENON@example.com"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Put_Returns204AndReplacesFields()
    {
        var client = await factory.CreateClientForAsync("hr");
        var created = await CreateAsync(client, "Put Target", "put.target@example.com");

        var response = await client.PutAsJsonAsync($"/api/employees/{created.Id}",
            new { name = "Put Target Renamed", salary = 75_000, departmentId = 1 });   // no email -> removed

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var fetched = await client.GetFromJsonAsync<EmployeeResponse>($"/api/employees/{created.Id}", TestJson.Options);
        Assert.Equal("Put Target Renamed", fetched!.Name);
        Assert.Null(fetched.Email);
        Assert.Equal(75_000m, fetched.Salary);
        Assert.Equal("Engineering", fetched.DepartmentName);
    }

    [Fact]
    public async Task Put_UnknownId_Returns404()
    {
        var client = await factory.CreateClientForAsync("hr");

        var response = await client.PutAsJsonAsync("/api/employees/9999", new { name = "Nobody Here", salary = 1, departmentId = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Patch_Returns200AndChangesOnlySentFields()
    {
        var client = await factory.CreateClientForAsync("hr");
        var created = await CreateAsync(client, "Patch Target", "patch.target@example.com");

        var response = await client.PatchAsJsonAsync($"/api/employees/{created.Id}", new { salary = 71_500 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var patched = await response.Content.ReadFromJsonAsync<EmployeeResponse>(TestJson.Options);
        Assert.Equal(71_500m, patched!.Salary);
        Assert.Equal("patch.target@example.com", patched.Email);   // unchanged
        Assert.Equal("Patch Target", patched.Name);
    }

    [Fact]
    public async Task Patch_EmptyBody_Returns400()
    {
        var client = await factory.CreateClientForAsync("hr");

        var response = await client.PatchAsJsonAsync("/api/employees/1", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ThenGet_Returns204Then404()
    {
        var hr = await factory.CreateClientForAsync("hr");
        var created = await CreateAsync(hr, "Delete Target");
        var admin = await factory.CreateClientForAsync("admin");

        var delete = await admin.DeleteAsync($"/api/employees/{created.Id}");
        var get = await admin.GetAsync($"/api/employees/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }
}

public class DepartmentEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Create_Returns201_DuplicateReturns409()
    {
        var admin = await factory.CreateClientForAsync("admin");

        var first = await admin.PostAsJsonAsync("/api/departments", new { name = "Legal" });
        var second = await admin.PostAsJsonAsync("/api/departments", new { name = "LEGAL" });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Delete_WithEmployees_Returns409()
    {
        var admin = await factory.CreateClientForAsync("admin");

        var response = await admin.DeleteAsync("/api/departments/1");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Empty_Returns204()
    {
        var admin = await factory.CreateClientForAsync("admin");
        var created = await (await admin.PostAsJsonAsync("/api/departments", new { name = "Temporary" }))
            .Content.ReadFromJsonAsync<DepartmentResponse>(TestJson.Options);

        var response = await admin.DeleteAsync($"/api/departments/{created!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Get_Unknown_Returns404()
    {
        var response = await factory.CreateClient().GetAsync("/api/departments/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
