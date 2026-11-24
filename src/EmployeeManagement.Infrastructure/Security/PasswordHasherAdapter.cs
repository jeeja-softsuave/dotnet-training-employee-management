using EmployeeManagement.Application.Abstractions;
using EmployeeManagement.Core.Models;
using Microsoft.AspNetCore.Identity;

namespace EmployeeManagement.Infrastructure.Security;

/// <summary>
/// Wraps ASP.NET Core Identity's PasswordHasher (PBKDF2 with a random salt per password).
/// Passwords are never stored, only these hashes.
/// </summary>
internal sealed class PasswordHasherAdapter : IPasswordHasher
{
    private readonly PasswordHasher<AppUser> _hasher = new();

    public string Hash(AppUser user, string password) => _hasher.HashPassword(user, password);

    public bool Verify(AppUser user, string passwordHash, string providedPassword) =>
        _hasher.VerifyHashedPassword(user, passwordHash, providedPassword) is
            PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
}
