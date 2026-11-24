namespace EmployeeManagement.Core.Models;

/// <summary>
/// A user who can sign in to the API. Only a password hash is stored, never the password.
/// Role is one of <see cref="Roles"/>.
/// </summary>
public sealed class AppUser
{
    public AppUser(int id, string userName, string role)
    {
        if (id < 0)
            throw new ArgumentOutOfRangeException(nameof(id), id, "User id cannot be negative.");
        if (string.IsNullOrWhiteSpace(userName))
            throw new ArgumentException("User name is required.", nameof(userName));
        if (!Roles.All.Contains(role))
            throw new ArgumentException($"Unknown role '{role}'.", nameof(role));

        Id = id;
        UserName = userName.Trim().ToLowerInvariant();
        Role = role;
    }

    public int Id { get; private set; }
    public string UserName { get; private set; }
    public string Role { get; private set; }
    public string PasswordHash { get; private set; } = string.Empty;

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        PasswordHash = passwordHash;
    }
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string HR = "HR";
    public const string Viewer = "Viewer";

    public static readonly IReadOnlySet<string> All = new HashSet<string> { Admin, HR, Viewer };
}
