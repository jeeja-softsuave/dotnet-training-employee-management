using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.Application.Dtos;

public sealed record DepartmentResponse(int Id, string Name, int Headcount);

public sealed class CreateDepartmentRequest
{
    [Required, StringLength(50, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
}

public sealed class LoginRequest
{
    [Required, StringLength(50)]
    public string UserName { get; set; } = string.Empty;

    [Required, StringLength(128)]
    public string Password { get; set; } = string.Empty;
}

public sealed record TokenResponse(string AccessToken, string TokenType, DateTimeOffset ExpiresAt, string UserName, string Role);

public sealed record CurrentUserResponse(string UserName, string Role, IReadOnlyList<ClaimResponse> Claims);

public sealed record ClaimResponse(string Type, string Value);
