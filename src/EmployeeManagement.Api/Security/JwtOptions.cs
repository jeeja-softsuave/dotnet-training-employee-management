using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.Api.Security;

/// <summary>
/// Options pattern: bound from the "Jwt" configuration section and validated at startup,
/// so a missing or weak signing key stops the app immediately with a clear message
/// instead of failing on the first login.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// HMAC-SHA256 key. This is a secret: never put it in appsettings.json or Git.
    /// Development: dotnet user-secrets. Production: environment variable Jwt__SigningKey or a vault.
    /// </summary>
    [Required(ErrorMessage = "Jwt:SigningKey is not set. Run: dotnet user-secrets set \"Jwt:SigningKey\" \"<32+ random characters>\" --project src/EmployeeManagement.Api")]
    [MinLength(32, ErrorMessage = "Jwt:SigningKey must be at least 32 characters (256 bits) for HMAC-SHA256.")]
    public string SigningKey { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int ExpiryMinutes { get; set; } = 60;
}
