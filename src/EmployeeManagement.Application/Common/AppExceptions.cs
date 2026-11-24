namespace EmployeeManagement.Application.Common;

/// <summary>
/// Exceptions the application layer throws for expected business outcomes.
/// The API's global exception handler turns each one into a ProblemDetails response
/// with the matching HTTP status code, so services never deal with HTTP themselves.
/// </summary>
public abstract class AppException(string message) : Exception(message);

/// <summary>Maps to 404 Not Found.</summary>
public sealed class NotFoundException(string resource, object key)
    : AppException($"{resource} with id {key} was not found.")
{
    public string Resource { get; } = resource;
    public object Key { get; } = key;
}

/// <summary>Maps to 409 Conflict: the request is valid but clashes with current state (duplicate, in use).</summary>
public sealed class ConflictException(string message) : AppException(message);

/// <summary>Maps to 403 Forbidden: the user is signed in but a business rule does not allow this action.</summary>
public sealed class ForbiddenException(string message) : AppException(message);

/// <summary>Maps to 400 Bad Request with field-level errors, same shape as model validation errors.</summary>
public sealed class BusinessRuleException : AppException
{
    public BusinessRuleException(string field, string message) : base(message)
    {
        Errors = new Dictionary<string, string[]> { [field] = [message] };
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
