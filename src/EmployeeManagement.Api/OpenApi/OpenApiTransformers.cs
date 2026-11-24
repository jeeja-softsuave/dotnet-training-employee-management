using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace EmployeeManagement.Api.OpenApi;

/// <summary>Adds the "Bearer" JWT security scheme so Swagger UI shows an Authorize button.</summary>
internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public const string SchemeName = "Bearer";

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info.Title = "Employee Management API";
        document.Info.Description =
            "Week 2 capstone API. Sign in with POST /api/auth/login, then click Authorize and paste the accessToken.";

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT access token from POST /api/auth/login.",
        };
        return Task.CompletedTask;
    }
}

/// <summary>
/// Marks every endpoint that is not [AllowAnonymous] as needing the Bearer token, and documents
/// the 401 / 403 responses the authorization layer can return.
/// </summary>
internal sealed class AuthorizationOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        if (metadata.OfType<IAllowAnonymous>().Any())
            return Task.CompletedTask;

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(BearerSecuritySchemeTransformer.SchemeName, context.Document)] = [],
        });

        operation.Responses ??= new OpenApiResponses();
        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized: no token, or the token is invalid or expired." });

        if (metadata.OfType<IAuthorizeData>().Any(a => a.Policy is not null || a.Roles is not null))
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden: signed in, but your role is not allowed to do this." });

        return Task.CompletedTask;
    }
}
