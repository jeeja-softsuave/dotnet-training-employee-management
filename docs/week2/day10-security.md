# Day 10: Security (Gate 2)

## Authentication vs authorization

| | Authentication | Authorization |
|---|---|---|
| Question | **Who are you?** | **What are you allowed to do?** |
| In this API | `POST /api/auth/login` checks the password and issues a **JWT**. `UseAuthentication()` validates that token on every request and builds `HttpContext.User` | `UseAuthorization()` checks `[Authorize(Policy = ...)]`, the fallback policy, and the raise rule in `EmployeeService` |
| Fails with | **401 Unauthorized** | **403 Forbidden** |

## 401 vs 403

- **401 Unauthorized**: the API doesn't know who you are.
  - Causes: no token, a tampered or expired token, or a wrong password at login.
  - The response includes `WWW-Authenticate: Bearer`, which tells the client to authenticate.
  - Fix: sign in.
- **403 Forbidden**: the API knows exactly who you are, and the answer is no.
  - Examples: Viewer tries to create an employee; HR tries to delete one; HR approves a 30% raise.
  - Signing in again won't help. You need a different role.

## JWT and claims

A JWT is `header.payload.signature`, Base64Url-encoded. The payload holds **claims**: facts about the
user that the API can trust because they're **signed** with the server's secret key. If anyone changes
one character, the signature check fails and the request gets 401 (tested in
`ProtectedEndpoint_TamperedToken_Returns401`).

Claims in our tokens ([JwtTokenService.cs](../../src/EmployeeManagement.Api/Security/JwtTokenService.cs)):

| Claim | Example | Used for |
|---|---|---|
| `sub` | `"2"` | User id |
| `name` | `"hr"` | `User.Identity.Name`, logs |
| `role` | `"HR"` | Role-based policies |
| `jti` | random | Unique token id |
| `iss`, `aud` | `EmployeeManagement.Api`, `EmployeeManagement.Clients` | Validated: the token was issued by us, for us |
| `exp`, `nbf`, `iat` | Unix times | Validated: lifetime 60 minutes, 30-second clock skew |

`GET /api/auth/me` shows the claims of the current token.

The token is signed with **HMAC-SHA256** (symmetric key, at least 32 characters, checked at startup).
The same key issues and validates tokens. If a separate identity provider issued tokens, you'd use an
asymmetric key (RS256) and validate with its public key instead.

## Roles and policies

| Role | Demo user | Can |
|---|---|---|
| Viewer | `viewer` | Read employees and the department summary |
| HR | `hr` | Viewer + create, PUT, PATCH, raises **up to 20%** |
| Admin | `admin` | Everything: raises up to 100%, delete employees, create and delete departments |

Defined in [AuthSetup.cs](../../src/EmployeeManagement.Api/Security/AuthSetup.cs):

```csharp
services.AddAuthorizationBuilder()
    // Secure by default: every endpoint needs a signed-in user unless it says [AllowAnonymous].
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
    .AddPolicy(Policies.CanManageEmployees, p => p.RequireRole(Roles.HR, Roles.Admin))
    .AddPolicy(Policies.AdminOnly, p => p.RequireRole(Roles.Admin));
```

- **Public** (`[AllowAnonymous]`): login, `GET /api/departments`, `GET /api/departments/{id}`,
  `/health`, and the OpenAPI docs (Development only).
- **Authorization business rule:** a role check can't say "HR up to 20%".
  [RaiseApprovalPolicy](../../src/EmployeeManagement.Application/Security/RaiseApprovalPolicy.cs)
  handles that in the application layer and throws `ForbiddenException`, which becomes 403. It's unit-tested at the boundary values 20 / 20.01 / 100 / 100.01.

## Demo: success, unauthenticated, forbidden

Captured with curl: [evidence/week2-api-demo.log](../evidence/week2-api-demo.log) (tokens masked).
Automated as integration tests: [AuthorizationTests.cs](../../tests/EmployeeManagement.Api.Tests/Integration/AuthorizationTests.cs).

| Request | As | Result |
|---|---|---|
| `GET /api/employees` | no token | **401** + `WWW-Authenticate: Bearer` |
| `GET /api/employees` | tampered token | **401** |
| `GET /api/employees?...` | viewer | **200** |
| `POST /api/employees` | viewer | **403** |
| `POST /api/employees` | hr | **201** |
| `POST /api/employees/{id}/raise` `{ "percentage": 30 }` | hr | **403** "You can approve raises up to 20%..." |
| same | admin | **200** |
| `DELETE /api/employees/{id}` | hr | **403** |
| same | admin | **204** |
| `GET /api/departments` | no token | **200** (public) |

## Passwords

- Only hashes are stored, made with ASP.NET Core Identity's `PasswordHasher` (PBKDF2, random salt per password).
- A failed login gives the **same message** whether the user name or the password was wrong. The
  service also runs a hash check for unknown users, so response times don't reveal which user names exist.

## Secrets and configuration

Nothing secret is committed. `appsettings.json` contains `"SigningKey": ""` on purpose.

| Setting | Secret? | Development | Production |
|---|---|---|---|
| `Jwt:SigningKey` | **Yes** | `dotnet user-secrets` (stored in `%APPDATA%\Microsoft\UserSecrets\<id>\secrets.json`, outside the repo) | Environment variable `Jwt__SigningKey`, or a vault (Azure Key Vault, AWS Secrets Manager) |
| `Database:DemoUserPassword` | Yes | `dotnet user-secrets` | Not set: no demo users in production |
| `ConnectionStrings:Default` | Not for SQLite (it would be for SQL Server with a password) | `appsettings.json` | Environment variable |
| `Jwt:Issuer`, `Audience`, `ExpiryMinutes` | No | `appsettings.json` | `appsettings.json` |

- The app **validates** `Jwt:SigningKey` at startup (required, at least 32 characters). If it's missing,
  startup stops with a message telling you which command to run.
- Integration tests use their own test-only key (`ApiFactory`), and the `Testing` environment doesn't
  load user secrets.

Setup commands are in the [README](../../README.md#week-2-web-api).

## HTTPS and CORS

- **HTTPS:** production enables HSTS and HTTPS redirection (`UseHsts`, `UseHttpsRedirection`), so tokens
  are never sent in clear text. Development uses plain HTTP on `localhost:5200` for simplicity. The
  `https` launch profile is also available.
- **CORS:** only the origins in `Cors:AllowedOrigins` may call the API from a browser
  (`http://localhost:3000` in Development; none by default). `X-Correlation-Id` and `Location` are
  exposed so the front end can read them.

## Not implemented (overview only)

- **Refresh tokens:** a long-lived token used to get new short-lived access tokens without asking for
  the password again. Here, the client simply signs in again after 60 minutes.
- **External identity provider** (Microsoft Entra ID, Auth0, Keycloak): it would issue the tokens, and
  this API would only validate them (`Authority` plus public keys instead of our own `SigningKey`).
- **Token revocation**, user management endpoints, rate limiting on login.
