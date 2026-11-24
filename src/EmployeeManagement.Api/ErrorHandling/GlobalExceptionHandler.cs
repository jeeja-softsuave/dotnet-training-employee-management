using EmployeeManagement.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Api.ErrorHandling;

/// <summary>
/// Central exception handling: turns exceptions into consistent ProblemDetails (RFC 9457)
/// responses. Controllers and services just throw; nobody writes try/catch for HTTP codes.
///
/// Expected business outcomes (not found, conflict...) are logged as warnings without a stack
/// trace. Anything unexpected is a 500: logged as an error with the stack trace, and the client
/// gets a generic message so internal details never leak.
/// </summary>
public sealed class GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = CreateProblem(exception);

        if (problem.Status >= 500)
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path.Value);
        else
            logger.LogWarning("{ExceptionType} for {Method} {Path}: {Message}",
                exception.GetType().Name, httpContext.Request.Method, httpContext.Request.Path.Value, exception.Message);

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    internal static ProblemDetails CreateProblem(Exception exception) => exception switch
    {
        NotFoundException e => Problem(StatusCodes.Status404NotFound, "Resource not found", e.Message),
        ConflictException e => Problem(StatusCodes.Status409Conflict, "Conflict", e.Message),
        ForbiddenException e => Problem(StatusCodes.Status403Forbidden, "Forbidden", e.Message),
        BusinessRuleException e => new ValidationProblemDetails(e.Errors.ToDictionary(kv => kv.Key, kv => kv.Value))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
        },
        // Domain entities validate themselves and throw ArgumentException (defence in depth).
        ArgumentException e => new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            [e.ParamName ?? "request"] = [StripParameterSuffix(e)],
        })
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
        },
        _ => Problem(StatusCodes.Status500InternalServerError, "An unexpected error occurred",
            "The error has been logged. Quote the correlationId when reporting it."),
    };

    private static ProblemDetails Problem(int status, string title, string detail) =>
        new() { Status = status, Title = title, Detail = detail };

    // "Salary must be ... (Parameter 'Salary')" -> "Salary must be ..."
    private static string StripParameterSuffix(ArgumentException e)
    {
        var message = e.Message;
        var suffix = $" (Parameter '{e.ParamName}')";
        var index = message.IndexOf(suffix, StringComparison.Ordinal);
        return index >= 0 ? message[..index].Trim() : message;
    }
}
