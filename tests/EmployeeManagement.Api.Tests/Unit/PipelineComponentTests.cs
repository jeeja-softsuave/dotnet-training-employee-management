using EmployeeManagement.Api.ErrorHandling;
using EmployeeManagement.Api.Middleware;
using EmployeeManagement.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Api.Tests.Unit;

public class RequestLoggingMiddlewareTests
{
    private readonly ListLogger<RequestLoggingMiddleware> _logger = new();

    private async Task<DefaultHttpContext> InvokeAsync(int statusCode, string? incomingCorrelationId = null, string path = "/api/employees", string query = "")
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = path;
        context.Request.QueryString = new QueryString(query);
        context.Request.Headers.Authorization = "Bearer secret-token-value";
        if (incomingCorrelationId is not null)
            context.Request.Headers[RequestLoggingMiddleware.CorrelationIdHeader] = incomingCorrelationId;

        var middleware = new RequestLoggingMiddleware(ctx =>
        {
            ctx.Response.StatusCode = statusCode;
            return Task.CompletedTask;
        }, _logger);

        await middleware.InvokeAsync(context);
        return context;
    }

    [Fact]
    public async Task Invoke_LogsMethodPathStatusAndDuration()
    {
        await InvokeAsync(200);

        var (level, message) = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Information, level);
        Assert.Matches(@"^HTTP GET /api/employees responded 200 in [\d.]+ ms$", message);
    }

    [Theory]
    [InlineData(404, LogLevel.Warning)]
    [InlineData(401, LogLevel.Warning)]
    [InlineData(500, LogLevel.Error)]
    public async Task Invoke_LogLevelFollowsStatusCode(int status, LogLevel expected)
    {
        await InvokeAsync(status);

        Assert.Equal(expected, _logger.Entries.Single().Level);
    }

    [Fact]
    public async Task Invoke_DoesNotLogTokenOrQueryString()
    {
        await InvokeAsync(200, query: "?search=salary-of-asha");

        var message = _logger.Entries.Single().Message;
        Assert.DoesNotContain("secret-token-value", message);
        Assert.DoesNotContain("salary-of-asha", message);
    }

    [Fact]
    public async Task Invoke_NoIncomingId_GeneratesOneAndStoresIt()
    {
        var context = await InvokeAsync(200);

        var id = Assert.IsType<string>(context.Items[RequestLoggingMiddleware.CorrelationIdItemKey]);
        Assert.Matches("^[0-9a-f]{32}$", id);
        Assert.Contains($"CorrelationId:{id}", _logger.Scopes);
    }

    [Fact]
    public async Task Invoke_SafeIncomingId_IsReused()
    {
        var context = await InvokeAsync(200, "client-req-42");

        Assert.Equal("client-req-42", context.Items[RequestLoggingMiddleware.CorrelationIdItemKey]);
    }

    [Theory]
    [InlineData("bad id with spaces")]
    [InlineData("line1\nFAKE LOG LINE")]
    public void ResolveCorrelationId_UnsafeIncoming_IsReplaced(string incoming)
    {
        var id = RequestLoggingMiddleware.ResolveCorrelationId(incoming);

        Assert.NotEqual(incoming, id);
        Assert.Matches("^[0-9a-f]{32}$", id);
    }

    [Fact]
    public void ResolveCorrelationId_TooLong_IsReplaced()
    {
        var incoming = new string('a', 65);

        Assert.NotEqual(incoming, RequestLoggingMiddleware.ResolveCorrelationId(incoming));
    }

    [Fact]
    public async Task Invoke_DownstreamThrows_LogsAs500AndRethrows()
    {
        var middleware = new RequestLoggingMiddleware(_ => throw new InvalidOperationException("boom"), _logger);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/x";

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));

        var (level, message) = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Error, level);
        Assert.Contains("responded 500", message);
    }
}

public class GlobalExceptionHandlerMappingTests
{
    [Fact]
    public void NotFound_Maps404()
    {
        var problem = GlobalExceptionHandler.CreateProblem(new NotFoundException("Employee", 7));

        Assert.Equal(404, problem.Status);
        Assert.Equal("Employee with id 7 was not found.", problem.Detail);
    }

    [Fact]
    public void Conflict_Maps409() =>
        Assert.Equal(409, GlobalExceptionHandler.CreateProblem(new ConflictException("dup")).Status);

    [Fact]
    public void Forbidden_Maps403() =>
        Assert.Equal(403, GlobalExceptionHandler.CreateProblem(new ForbiddenException("no")).Status);

    [Fact]
    public void BusinessRule_Maps400WithFieldErrors()
    {
        var problem = GlobalExceptionHandler.CreateProblem(new BusinessRuleException("DepartmentId", "Department 9 does not exist."));

        var validation = Assert.IsType<ValidationProblemDetails>(problem);
        Assert.Equal(400, validation.Status);
        Assert.Equal(["Department 9 does not exist."], validation.Errors["DepartmentId"]);
    }

    [Fact]
    public void DomainArgumentException_Maps400WithoutParameterSuffix()
    {
        var problem = GlobalExceptionHandler.CreateProblem(new ArgumentException("Name is required.", "Name"));

        var validation = Assert.IsType<ValidationProblemDetails>(problem);
        Assert.Equal(["Name is required."], validation.Errors["Name"]);
    }

    [Fact]
    public void UnexpectedException_Maps500WithoutLeakingDetails()
    {
        var problem = GlobalExceptionHandler.CreateProblem(new InvalidOperationException("connection string Password=hunter2"));

        Assert.Equal(500, problem.Status);
        Assert.DoesNotContain("hunter2", problem.Detail);
    }
}
