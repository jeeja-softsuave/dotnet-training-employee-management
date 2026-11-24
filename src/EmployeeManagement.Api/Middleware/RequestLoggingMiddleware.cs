using System.Diagnostics;
using System.Text.RegularExpressions;

namespace EmployeeManagement.Api.Middleware;

/// <summary>
/// Custom middleware: gives every request a correlation id and logs one line per request
/// with method, path, status code and duration.
///
/// Logged safely: no request/response bodies, no headers (the Authorization header holds the
/// token), no query string (it can contain search terms or personal data). An incoming
/// correlation id is only accepted if it is short and contains safe characters, so a client
/// cannot inject fake lines into the logs.
///
/// Registered first in the pipeline so it also sees the final status code of requests that
/// failed and were turned into a 500 by the exception handler.
/// </summary>
public sealed partial class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public const string CorrelationIdHeader = "X-Correlation-Id";
    public const string CorrelationIdItemKey = "CorrelationId";

    [GeneratedRegex("^[A-Za-z0-9._-]{1,64}$")]
    private static partial Regex SafeCorrelationId();

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context.Request.Headers[CorrelationIdHeader].ToString());
        context.Items[CorrelationIdItemKey] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        // Log scope: every log line written during this request (services, EF Core SQL...)
        // carries CorrelationId as a structured property.
        using var scope = logger.BeginScope("CorrelationId:{CorrelationId}", correlationId);

        var started = Stopwatch.GetTimestamp();
        var failed = false;
        try
        {
            await next(context);
        }
        catch
        {
            failed = true;   // only if nothing further in the pipeline handled it
            throw;
        }
        finally
        {
            var statusCode = failed ? StatusCodes.Status500InternalServerError : context.Response.StatusCode;
            var elapsedMs = Math.Round(Stopwatch.GetElapsedTime(started).TotalMilliseconds, 1);
            var level = statusCode >= 500 ? LogLevel.Error : statusCode >= 400 ? LogLevel.Warning : LogLevel.Information;

            logger.Log(level, "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs} ms",
                context.Request.Method, context.Request.Path.Value, statusCode, elapsedMs);
        }
    }

    internal static string ResolveCorrelationId(string? incoming) =>
        !string.IsNullOrEmpty(incoming) && SafeCorrelationId().IsMatch(incoming)
            ? incoming
            : Guid.NewGuid().ToString("N");
}
