using EmployeeManagement.Application.Abstractions;
using EmployeeManagement.Application.Dtos;
using EmployeeManagement.Core.Models;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Application.Services;

public interface IAuthService
{
    /// <returns>A token, or null if the user name or password is wrong.</returns>
    Task<TokenResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}

public sealed class AuthService(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    ILogger<AuthService> logger) : IAuthService
{
    // Used when the user does not exist, so a failed login takes about as long either way.
    // Otherwise response time would reveal which user names exist.
    private static readonly AppUser DummyUser = new(0, "dummy", Roles.Viewer);
    private string? _dummyHash;

    public async Task<TokenResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userName = request.UserName.Trim().ToLowerInvariant();
        var user = await users.GetByUserNameAsync(userName, cancellationToken);

        if (user is null)
        {
            _dummyHash ??= passwordHasher.Hash(DummyUser, "not-a-real-password");
            passwordHasher.Verify(DummyUser, _dummyHash, request.Password);
            logger.LogWarning("Failed login for unknown user {UserName}", userName);
            return null;
        }

        if (!passwordHasher.Verify(user, user.PasswordHash, request.Password))
        {
            logger.LogWarning("Failed login for user {UserName}: wrong password", userName);
            return null;
        }

        var token = tokenService.CreateToken(user);
        logger.LogInformation("User {UserName} signed in with role {Role}", user.UserName, user.Role);

        return new TokenResponse(token.Token, "Bearer", token.ExpiresAt, user.UserName, user.Role);
    }
}
