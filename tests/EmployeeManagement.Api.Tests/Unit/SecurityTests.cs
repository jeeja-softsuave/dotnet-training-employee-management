using EmployeeManagement.Api.Security;
using EmployeeManagement.Application.Security;
using EmployeeManagement.Core.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace EmployeeManagement.Api.Tests.Unit;

public class RaiseApprovalPolicyTests
{
    private static Func<string, bool> RolesOf(params string[] roles) => roles.Contains;

    [Fact]
    public void MaxApprovable_ByRole()
    {
        Assert.Equal(100m, RaiseApprovalPolicy.MaxApprovablePercentage(RolesOf(Roles.Admin)));
        Assert.Equal(20m, RaiseApprovalPolicy.MaxApprovablePercentage(RolesOf(Roles.HR)));
        Assert.Equal(0m, RaiseApprovalPolicy.MaxApprovablePercentage(RolesOf(Roles.Viewer)));
        Assert.Equal(0m, RaiseApprovalPolicy.MaxApprovablePercentage(RolesOf()));
    }

    [Fact]
    public void AdminAndHrTogether_GetsTheHigherAdminLimit()
    {
        Assert.Equal(100m, RaiseApprovalPolicy.MaxApprovablePercentage(RolesOf(Roles.HR, Roles.Admin)));
    }

    [Theory]
    [InlineData(Roles.HR, 20, true)]
    [InlineData(Roles.HR, 20.01, false)]
    [InlineData(Roles.Admin, 100, true)]
    [InlineData(Roles.Admin, 100.01, false)]
    [InlineData(Roles.Admin, 0, false)]
    [InlineData(Roles.Viewer, 0.01, false)]
    public void CanApprove_AtAndAroundLimits(string role, decimal percentage, bool expected)
    {
        Assert.Equal(expected, RaiseApprovalPolicy.CanApprove(percentage, RolesOf(role)));
    }
}

public class JwtTokenServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 24, 10, 0, 0, TimeSpan.Zero);

    private static readonly JwtOptions Options = new()
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        SigningKey = "unit-test-signing-key-0123456789abcdef",
        ExpiryMinutes = 30,
    };

    private static JwtTokenService CreateService() =>
        new(Microsoft.Extensions.Options.Options.Create(Options), new FixedTimeProvider(Now));

    [Fact]
    public void CreateToken_ExpiresAfterConfiguredMinutes()
    {
        var token = CreateService().CreateToken(new AppUser(7, "hr", Roles.HR));

        Assert.Equal(Now.AddMinutes(30), token.ExpiresAt);
    }

    [Fact]
    public void CreateToken_ContainsUserClaims()
    {
        var token = CreateService().CreateToken(new AppUser(7, "hr", Roles.HR));

        var jwt = new JsonWebToken(token.Token);
        Assert.Equal("7", jwt.Subject);
        Assert.Equal("hr", jwt.GetClaim(AppClaimTypes.Name).Value);
        Assert.Equal(Roles.HR, jwt.GetClaim(AppClaimTypes.Role).Value);
        Assert.Equal("test-issuer", jwt.Issuer);
        Assert.Contains("test-audience", jwt.Audiences);
    }

    [Fact]
    public async Task CreateToken_IsSignedWithConfiguredKey()
    {
        var token = CreateService().CreateToken(new AppUser(1, "admin", Roles.Admin));

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Token, new TokenValidationParameters
        {
            ValidIssuer = Options.Issuer,
            ValidAudience = Options.Audience,
            IssuerSigningKey = JwtTokenService.CreateSigningKey(Options.SigningKey),
            ValidateLifetime = false,   // fixed clock in 2026
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Token_WithWrongKey_FailsValidation()
    {
        var token = CreateService().CreateToken(new AppUser(1, "admin", Roles.Admin));

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Token, new TokenValidationParameters
        {
            ValidIssuer = Options.Issuer,
            ValidAudience = Options.Audience,
            IssuerSigningKey = JwtTokenService.CreateSigningKey("a-completely-different-key-0123456789"),
            ValidateLifetime = false,
        });

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("too-short-key")]
    public void JwtOptions_WeakSigningKey_FailsValidation(string key)
    {
        var options = new JwtOptions { Issuer = "i", Audience = "a", SigningKey = key };

        var errors = Validate.ErrorFields(options);

        Assert.Contains("SigningKey", errors);
    }
}
