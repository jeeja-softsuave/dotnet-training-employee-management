using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EmployeeManagement.Application.Dtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace EmployeeManagement.Api.Tests.Integration;

/// <summary>
/// Starts the real API in memory (full middleware pipeline, DI, EF Core, JWT) against a private
/// in-memory SQLite database. Each test class gets its own factory, so its own database.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string DemoPassword = "Integration-Test-Password-1";

    private readonly string _connectionString = $"Data Source=file:emtests-{Guid.NewGuid():N}?mode=memory&cache=shared";
    private readonly SqliteConnection _keepAlive;

    public ApiFactory()
    {
        // A shared in-memory SQLite database lives only while a connection is open.
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _connectionString);
        builder.UseSetting("Jwt:SigningKey", "integration-tests-signing-key-0123456789abcdef");
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.UseSetting("Database:SeedDemoData", "true");
        builder.UseSetting("Database:DemoUserPassword", DemoPassword);
        builder.UseSetting("Logging:LogLevel:Default", "Warning");
    }

    public async Task<HttpClient> CreateClientForAsync(string userName)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { userName, password = DemoPassword });
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(TestJson.Options);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _keepAlive.Dispose();
    }
}

internal static class TestJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}
