using System.Security.Claims;
using System.Text;
using EmployeeManagement.Application.Abstractions;
using EmployeeManagement.Core.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace EmployeeManagement.Api.Security;

/// <summary>Claim names used in our tokens. Short JWT-standard names; no inbound claim mapping.</summary>
public static class AppClaimTypes
{
    public const string Subject = JwtRegisteredClaimNames.Sub;
    public const string Name = "name";
    public const string Role = "role";
}

/// <summary>
/// Creates signed JWT access tokens.
/// Singleton: it has no per-request state, and its dependencies (IOptions, TimeProvider) are
/// singletons too. It must NOT depend on a scoped service such as the DbContext; that would be
/// a captive dependency (the scoped object would live for the whole app).
/// </summary>
public sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider) : ITokenService
{
    private readonly JsonWebTokenHandler _handler = new();

    public AccessToken CreateToken(AppUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var jwt = options.Value;
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(jwt.ExpiryMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            // Claims = facts about the user that the API can trust because the token is signed.
            Subject = new ClaimsIdentity(
            [
                new Claim(AppClaimTypes.Subject, user.Id.ToString()),
                new Claim(AppClaimTypes.Name, user.UserName),
                new Claim(AppClaimTypes.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            ]),
            SigningCredentials = new SigningCredentials(CreateSigningKey(jwt.SigningKey), SecurityAlgorithms.HmacSha256),
        };

        return new AccessToken(_handler.CreateToken(descriptor), expiresAt);
    }

    public static SymmetricSecurityKey CreateSigningKey(string signingKey) => new(Encoding.UTF8.GetBytes(signingKey));
}
