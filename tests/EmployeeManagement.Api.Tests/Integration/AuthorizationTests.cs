using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EmployeeManagement.Application.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Api.Tests.Integration;

/// <summary>
/// Gate 2 demo as tests: authorized (2xx), unauthenticated (401) and forbidden (403).
/// </summary>
public class AuthorizationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static object NewEmployee(string name) => new
    {
        name,
        type = "FullTime",
        salary = 50_000,
        hireDate = "2026-01-15",
        departmentId = 1,
    };

    // ---------- authentication ----------

    [Fact]
    public async Task Login_CorrectPassword_Returns200WithToken()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { userName = "hr", password = ApiFactory.DemoPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(TestJson.Options);
        Assert.Equal("HR", token!.Role);
        Assert.Equal("Bearer", token.TokenType);
        Assert.Equal(3, token.AccessToken.Split('.').Length);   // header.payload.signature
    }

    [Theory]
    [InlineData("hr", "wrong-password")]
    [InlineData("nobody", "whatever")]
    public async Task Login_BadCredentials_Returns401WithSameMessage(string userName, string password)
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { userName, password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Invalid user name or password.", problem!.Detail);
    }

    [Fact]
    public async Task ProtectedEndpoint_NoToken_Returns401WithBearerChallenge()
    {
        var response = await factory.CreateClient().GetAsync("/api/employees");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Bearer", response.Headers.WwwAuthenticate.Single().Scheme);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ProtectedEndpoint_TamperedToken_Returns401()
    {
        var client = await factory.CreateClientForAsync("admin");
        var token = client.DefaultRequestHeaders.Authorization!.Parameter!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token[..^4] + "AAAA");

        var response = await client.GetAsync("/api/employees");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PublicEndpoint_NoToken_Returns200()
    {
        var response = await factory.CreateClient().GetAsync("/api/departments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Me_ReturnsClaimsFromToken()
    {
        var client = await factory.CreateClientForAsync("viewer");

        var me = await client.GetFromJsonAsync<CurrentUserResponse>("/api/auth/me", TestJson.Options);

        Assert.Equal("viewer", me!.UserName);
        Assert.Equal("Viewer", me.Role);
        Assert.Contains(me.Claims, c => c.Type == "role" && c.Value == "Viewer");
    }

    // ---------- authorization ----------

    [Fact]
    public async Task Viewer_CanRead_Returns200()
    {
        var client = await factory.CreateClientForAsync("viewer");

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/employees")).StatusCode);
    }

    [Fact]
    public async Task Viewer_CreateEmployee_Returns403()
    {
        var client = await factory.CreateClientForAsync("viewer");

        var response = await client.PostAsJsonAsync("/api/employees", NewEmployee("Viewer Attempt"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Hr_CreateEmployee_Returns201()
    {
        var client = await factory.CreateClientForAsync("hr");

        var response = await client.PostAsJsonAsync("/api/employees", NewEmployee("Hr Created"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Hr_DeleteEmployee_Returns403_AdminReturns204()
    {
        var hr = await factory.CreateClientForAsync("hr");
        var created = await (await hr.PostAsJsonAsync("/api/employees", NewEmployee("To Be Deleted")))
            .Content.ReadFromJsonAsync<EmployeeResponse>(TestJson.Options);

        var hrDelete = await hr.DeleteAsync($"/api/employees/{created!.Id}");
        var admin = await factory.CreateClientForAsync("admin");
        var adminDelete = await admin.DeleteAsync($"/api/employees/{created.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, hrDelete.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, adminDelete.StatusCode);
    }

    [Fact]
    public async Task Hr_CreateDepartment_Returns403()
    {
        var client = await factory.CreateClientForAsync("hr");

        var response = await client.PostAsJsonAsync("/api/departments", new { name = "Legal" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Hr_RaiseAboveTwentyPercent_Returns403WithReason()
    {
        var client = await factory.CreateClientForAsync("hr");

        var response = await client.PostAsJsonAsync("/api/employees/2/raise", new { percentage = 25 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Contains("up to 20%", problem!.Detail);
    }

    [Fact]
    public async Task Admin_RaiseAboveTwentyPercent_Returns200()
    {
        var client = await factory.CreateClientForAsync("admin");

        var response = await client.PostAsJsonAsync("/api/employees/9/raise", new { percentage = 25 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var employee = await response.Content.ReadFromJsonAsync<EmployeeResponse>(TestJson.Options);
        Assert.Equal(82_500m, employee!.Salary);   // Sneha Iyer: 66,000 * 1.25
    }
}
