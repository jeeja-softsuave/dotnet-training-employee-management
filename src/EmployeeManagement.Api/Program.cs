using System.Diagnostics;
using System.Text.Json.Serialization;
using EmployeeManagement.Api.ErrorHandling;
using EmployeeManagement.Api.Middleware;
using EmployeeManagement.Api.OpenApi;
using EmployeeManagement.Api.Security;
using EmployeeManagement.Application;
using EmployeeManagement.Infrastructure;
using EmployeeManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);
var isLocal = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing");

// ---------------------------------------------------------------------------
// Services (dependency injection container)
// ---------------------------------------------------------------------------

// Fail fast on lifetime mistakes: resolving a scoped service from a singleton (captive dependency)
// or a missing registration throws at startup instead of at the first request.
builder.Host.UseDefaultServiceProvider(o =>
{
    o.ValidateScopes = true;
    o.ValidateOnBuild = true;
});

// Singleton: one shared clock for the whole app. Tests can swap in a fake time provider.
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddApplication();                                                    // Scoped services
builder.Services.AddInfrastructure(ResolveConnectionString(builder));                // Scoped DbContext + repositories, transient hasher
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.SectionName));
builder.Services.AddApiSecurity(builder.Configuration);                              // JWT, policies, singleton token service

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Every error (validation, 404, 401/403, exceptions) uses the same ProblemDetails JSON shape,
// with traceId and correlationId so a client report can be matched to the logs.
builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
{
    ctx.ProblemDetails.Instance ??= $"{ctx.HttpContext.Request.Method} {ctx.HttpContext.Request.Path}";
    ctx.ProblemDetails.Extensions["traceId"] = Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier;
    if (ctx.HttpContext.Items[RequestLoggingMiddleware.CorrelationIdItemKey] is string correlationId)
        ctx.ProblemDetails.Extensions["correlationId"] = correlationId;
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddOpenApi(o =>
{
    o.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    o.AddOperationTransformer<AuthorizationOperationTransformer>();
});

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithExposedHeaders(RequestLoggingMiddleware.CorrelationIdHeader, "Location")));

builder.Services.AddHealthChecks();

var app = builder.Build();

// ---------------------------------------------------------------------------
// Database: migrate + seed (only where configured: Development / Testing)
// ---------------------------------------------------------------------------
var databaseOptions = builder.Configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();
if (databaseOptions.MigrateOnStartup || databaseOptions.SeedDemoData)
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync(databaseOptions);
}

// ---------------------------------------------------------------------------
// Middleware pipeline. ORDER MATTERS: each request flows top to bottom, the response bottom to top.
// ---------------------------------------------------------------------------
app.UseMiddleware<RequestLoggingMiddleware>();   // 1. outermost: correlation id + logs the FINAL status
app.UseExceptionHandler();                       // 2. catches exceptions from everything below -> ProblemDetails
app.UseStatusCodePages();                        // 3. empty 401/403/404 responses get a ProblemDetails body

if (!isLocal)
{
    app.UseHsts();
    app.UseHttpsRedirection();                   // 4. production: force HTTPS
}

if (isLocal)
{
    // Before authentication so the docs themselves don't need a token.
    app.UseSwaggerUI(o =>
    {
        o.SwaggerEndpoint("/openapi/v1.json", "Employee Management API v1");
        o.RoutePrefix = "swagger";
    });
}

app.UseCors();                                   // 5. before auth, so pre-flight OPTIONS requests are answered
app.UseAuthentication();                         // 6. reads the bearer token -> HttpContext.User
app.UseAuthorization();                          // 7. checks [Authorize] policies / fallback policy

if (isLocal)
    app.MapOpenApi().AllowAnonymous();           // /openapi/v1.json

app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();                            // 8. endpoint: model binding, validation, the action

app.Run();

// Resolves a relative SQLite file path against the project folder, so the database is always
// created in the same place no matter which folder "dotnet run" is started from.
static string ResolveConnectionString(WebApplicationBuilder builder)
{
    var connectionString = builder.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

    var csb = new SqliteConnectionStringBuilder(connectionString);
    var isInMemory = csb.Mode == SqliteOpenMode.Memory || csb.DataSource == ":memory:" || csb.DataSource.StartsWith("file:", StringComparison.Ordinal);
    if (!isInMemory && !Path.IsPathRooted(csb.DataSource))
        csb.DataSource = Path.Combine(builder.Environment.ContentRootPath, csb.DataSource);

    return csb.ToString();
}

/// <summary>Exposed so integration tests can start the app with WebApplicationFactory&lt;Program&gt;.</summary>
public partial class Program;
