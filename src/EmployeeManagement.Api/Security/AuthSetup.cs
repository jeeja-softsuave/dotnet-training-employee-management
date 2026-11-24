using EmployeeManagement.Application.Abstractions;
using EmployeeManagement.Core.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EmployeeManagement.Api.Security;

public static class Policies
{
    /// <summary>HR or Admin: create, update and give raises.</summary>
    public const string CanManageEmployees = nameof(CanManageEmployees);

    /// <summary>Admin only: delete employees, create and delete departments.</summary>
    public const string AdminOnly = nameof(AdminOnly);
}

/// <summary>
/// Configures JWT validation from <see cref="JwtOptions"/>. Done through IConfigureOptions so
/// the key is read when first needed (after startup validation), not while building services.
/// </summary>
internal sealed class ConfigureJwtBearerOptions(IOptions<JwtOptions> jwtOptions) : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(string? name, JwtBearerOptions options) => Configure(options);

    public void Configure(JwtBearerOptions options)
    {
        var jwt = jwtOptions.Value;

        // Keep claim names exactly as issued ("role", "name") instead of long WS-* URIs.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = JwtTokenService.CreateSigningKey(jwt.SigningKey),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = AppClaimTypes.Name,
            RoleClaimType = AppClaimTypes.Role,
        };
    }
}

/// <summary>Reads the signed-in user from the current request's claims.</summary>
internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private System.Security.Claims.ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
    public string? UserName => Principal?.Identity?.Name;
    public bool IsInRole(string role) => Principal?.IsInRole(role) == true;
}

public static class AuthServiceCollectionExtensions
{
    public static IServiceCollection AddApiSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        // Authentication = WHO are you? (validate the bearer token)
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.ConfigureOptions<ConfigureJwtBearerOptions>();

        // Authorization = WHAT may you do? (roles / policies)
        services.AddAuthorizationBuilder()
            // Secure by default: every endpoint needs a signed-in user unless marked [AllowAnonymous].
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(Policies.CanManageEmployees, p => p.RequireRole(Roles.HR, Roles.Admin))
            .AddPolicy(Policies.AdminOnly, p => p.RequireRole(Roles.Admin));

        return services;
    }
}
