using EmployeeManagement.Api.Security;
using EmployeeManagement.Application.Dtos;
using EmployeeManagement.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController(IAuthService auth) : ControllerBase
{
    /// <summary>Signs in and returns a JWT access token.</summary>
    /// <response code="401">Wrong user name or password (the message doesn't say which).</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var token = await auth.LoginAsync(request, cancellationToken);
        return token is null
            ? Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Sign-in failed", detail: "Invalid user name or password.")
            : Ok(token);
    }

    /// <summary>Shows the signed-in user and the claims in their token.</summary>
    [HttpGet("me")]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    public ActionResult<CurrentUserResponse> Me() => Ok(new CurrentUserResponse(
        User.Identity?.Name ?? string.Empty,
        User.FindFirst(AppClaimTypes.Role)?.Value ?? string.Empty,
        User.Claims.Select(c => new ClaimResponse(c.Type, c.Value)).ToList()));
}
