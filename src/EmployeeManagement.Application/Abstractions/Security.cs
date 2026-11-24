using EmployeeManagement.Core.Models;

namespace EmployeeManagement.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(AppUser user, string password);
    bool Verify(AppUser user, string passwordHash, string providedPassword);
}

public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);

public interface ITokenService
{
    AccessToken CreateToken(AppUser user);
}

/// <summary>Who is making the current request. Implemented in the API from the JWT claims.</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    string? UserName { get; }
    bool IsInRole(string role);
}
