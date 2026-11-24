# Employee & Department Management

Capstone project for the .NET training programme (Weeks 1–3), domain **Employee & Department Management**.

- **Week 1:** C# console application: domain model, interfaces, LINQ, async with cancellation, xUnit tests.
- **Week 2:** ASP.NET Core Web API on the same domain: REST endpoints, layered architecture, DI,
  middleware, EF Core with SQLite, validation, ProblemDetails errors, and JWT authentication with
  role-based authorization. Jump to [Week 2: Web API](#week-2-web-api).

## Structure

| Project | Layer | Purpose |
|---|---|---|
| `src/EmployeeManagement.Core` | Domain | Entities and domain rules (Week 1), reused by the API |
| `src/EmployeeManagement.App` | Presentation | Week 1 console application |
| `src/EmployeeManagement.Application` | Application | Week 2 services, DTOs, validation, repository interfaces, query rules |
| `src/EmployeeManagement.Infrastructure` | Infrastructure | Week 2 EF Core DbContext, repositories, migrations, password hashing, seeding |
| `src/EmployeeManagement.Api` | Presentation | Week 2 ASP.NET Core Web API |
| `tests/EmployeeManagement.Tests` | Tests | Week 1 unit tests (142) |
| `tests/EmployeeManagement.Api.Tests` | Tests | Week 2 unit and integration tests (165) |

```
src/EmployeeManagement.Core
├── Models/        Employee (abstract), FullTimeEmployee, ContractEmployee, Department (record),
│                  SalaryRange (readonly record struct), EmploymentType (enum), AppUser, report records
├── Services/      IEmployeeRepository, IDepartmentRepository, IEmployeeDataSource,
│                  InMemory* implementations, EmployeeService, SampleData
├── Queries/       EmployeeQueries — 13 LINQ extension methods
├── Formatting/    EmployeeFormatter — pattern matching, StringBuilder report
└── Exceptions/    EmployeeNotFound, DepartmentNotFound, DuplicateEmployee
```

## Prerequisites

- .NET SDK 10.0 (`dotnet --version` prints `10.0.x`)

## Build and test everything

```bash
dotnet restore
dotnet build
dotnet test
```

## Week 1: Console application

```bash
dotnet run --project src/EmployeeManagement.App
```

On start-up the app loads 10 sample employees asynchronously (about 1 second). Press **Ctrl+C** during
loading to cancel it, and the app starts with an empty list. One sample record references a department
that doesn't exist, so it's skipped on purpose.

## Menu

```
1. List all employees        6. Give a raise
2. Add an employee           7. Remove an employee
3. Search by name            8. Department report
4. Filter by department      9. Top earners
5. Filter by salary range    0. Exit
```

Invalid input (non-numbers, out-of-range values, unknown department, bad dates) is re-prompted;
business errors such as an unknown employee id are reported without crashing.

## Sample inputs and outputs

The menu that is printed between commands is omitted below. Numbers are shown in the machine's culture
(en-IN, which groups as 1,60,000.00).

### 1. Filter by department

**Input:** `4` (filter by department) → `1` (Engineering) → `0` (exit)

**Output:**
```
Departments: 1 = Engineering, 2 = Human Resources, 3 = Sales, 4 = Finance
Department id: Id    Name                 Dept     Type            Salary
#1    Asha Menon           Dept 1   FullTime     95,000.00
#2    Ravi Kumar           Dept 1   FullTime     72,000.00
#3    Priya Nair           Dept 1   Contract     60,000.00
#8    Vikram Rao           Dept 1   FullTime   1,60,000.00
#10   Thomas Mathew        Dept 1   Contract     52,000.00
(5 employee(s))
```

### 2. Add an employee (with invalid input), give a raise, search

**Input:** `2` → name `Kiran Shah` → type `x` (invalid) → `f` → salary `abc` (invalid) → `70000` →
hire date `2026-11-01` → department `9` (does not exist) → `3` → email `kiran.shah@example.com`;
then `6` → id `11` → `10`%; then `3` → `kiran`

**Output:**
```
Type (F = full-time, C = contract):   Please enter one of: F, C.
Annual salary:   Please enter a number between 0.01 and 1,00,00,000.00.
Department id:   Department 9 does not exist.
[event] Hired Kiran Shah (#11).
Added: Kiran Shah: full-time (band B)
[event] Kiran Shah: salary 70,000.00 -> 77,000.00.
Id    Name                 Dept     Type            Salary
#11   Kiran Shah           Dept 3   FullTime     77,000.00
(1 employee(s))
```

### 3. Reports, salary range, and an error case

**Input:** `8` (department report); `9` → `3` (top 3 earners); `5` → `60000` → `72000` (salary range);
`6` → `404` → `10` (raise for an employee that does not exist)

**Output:**
```
Department         Headcount    Total salary      Average
---------------------------------------------------------
Engineering                5     4,39,000.00    87,800.00
Sales                      3     1,99,000.00    66,333.33
Human Resources            2     1,02,000.00    51,000.00
Finance                    0            0.00         0.00

Full-time: 7, Contract: 3
Total annual cost (salary + benefits/fees): 8,72,300.00
Missing email: Priya Nair, Meera Pillai

1. Vikram Rao           Engineering       1,60,000.00
2. Asha Menon           Engineering         95,000.00
3. Arjun Das            Sales               88,000.00

#2    Ravi Kumar           Dept 1   FullTime     72,000.00
#9    Sneha Iyer           Dept 3   FullTime     66,000.00
#3    Priya Nair           Dept 1   Contract     60,000.00
(3 employee(s))

Error: Employee with id 404 was not found.
```

The salary range filter includes both bounds (60,000 and 72,000). See defect DEF-001 in
[docs/day5-walkthrough.md](docs/day5-walkthrough.md).

## Week 1 deliverables

| Day | Deliverable | Where |
|---|---|---|
| 1 | Console app processing employee records | `src/EmployeeManagement.App` |
| 1 | Value vs reference types note | [docs/day1-value-vs-reference.md](docs/day1-value-vs-reference.md) |
| 2 | Domain class library (3+ classes), interface + implementation | `src/EmployeeManagement.Core/Models`, `Services` |
| 2 | Class diagram + SOLID explanation | [docs/day2-design-solid.md](docs/day2-design-solid.md) |
| 3 | 13 LINQ queries, unit-testable | `src/EmployeeManagement.Core/Queries/EmployeeQueries.cs` |
| 3 | Input, output and complexity per query | [docs/day3-linq.md](docs/day3-linq.md) |
| 4 | App, class library and xUnit test projects | `EmployeeManagement.sln` |
| 4 | Build and test log | [docs/evidence/day4-build-test.log](docs/evidence/day4-build-test.log) |
| 4 | Async method with cancellation | `EmployeeService.ImportAsync`, `SampleEmployeeDataSource` |
| 5 | Tests for normal, boundary and invalid inputs | `tests/EmployeeManagement.Tests` |
| 5 | Regression test for an injected defect | [docs/day5-walkthrough.md](docs/day5-walkthrough.md#defect-def-001) |
| 5 | Walkthrough: one class, one LINQ query, one async operation | [docs/day5-walkthrough.md](docs/day5-walkthrough.md) |
| 1–5 | LeetCode log | [leetcode/README.md](leetcode/README.md) |

---

## Week 2: Web API

ASP.NET Core Web API for the same domain, backed by **SQLite** through **EF Core 10**, protected with
**JWT bearer tokens**. Architecture: Controller → Service → Repository → EF Core
([diagram](docs/week2/day08-architecture.md)).

### 1. One-time setup

```bash
# EF Core command-line tool (version pinned in dotnet-tools.json)
dotnet tool restore

# Secrets: stored outside the repo by dotnet user-secrets, never in appsettings.json
dotnet user-secrets set "Jwt:SigningKey" "<at least 32 random characters>" --project src/EmployeeManagement.Api
dotnet user-secrets set "Database:DemoUserPassword" "<a password for the demo users>" --project src/EmployeeManagement.Api
```

To generate a cryptographically random signing key in PowerShell:

```powershell
$b = New-Object byte[] 48; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); [Convert]::ToBase64String($b)
```

The API refuses to start if `Jwt:SigningKey` is missing or shorter than 32 characters, and the error
message says which command to run.

### 2. Database

Nothing to install: SQLite creates `src/EmployeeManagement.Api/employees.db`.

- **Development:** the API applies migrations and seeds the Week 1 sample data (4 departments,
  10 employees) plus the demo users `admin`, `hr` and `viewer` automatically on first start.
- **Explicitly** (as in production), apply the migrations with:
  ```bash
  dotnet ef database update -p src/EmployeeManagement.Infrastructure -s src/EmployeeManagement.Api
  ```
- **Reset:** stop the API and delete `src/EmployeeManagement.Api/employees.db*`.

Details: [docs/week2/day09-ef-core.md](docs/week2/day09-ef-core.md).

### 3. Run

```bash
dotnet run --project src/EmployeeManagement.Api
```

| URL | What |
|---|---|
| <http://localhost:5200/swagger> | Swagger UI. Call `POST /api/auth/login`, click **Authorize**, paste the `accessToken` |
| <http://localhost:5200/openapi/v1.json> | OpenAPI document (saved copy: [docs/evidence/openapi-v1.json](docs/evidence/openapi-v1.json)) |
| <http://localhost:5200/health> | Health check |

**Example requests** for every endpoint, with success and failure cases:
[src/EmployeeManagement.Api/EmployeeManagement.Api.http](src/EmployeeManagement.Api/EmployeeManagement.Api.http).
Open it in VS Code (REST Client extension), Visual Studio or Rider, set `@password`, and run the login requests first.

### 4. Users and permissions

| User | Role | Can |
|---|---|---|
| `viewer` | Viewer | Read employees and the department summary |
| `hr` | HR | + create, PUT, PATCH, raises up to 20% |
| `admin` | Admin | Everything: raises up to 100%, delete employees, create and delete departments |

`GET /api/departments`, login and `/health` are public. Everything else needs a token.

### 5. Endpoints

| Verb | Route | Success |
|---|---|---|
| POST | `/api/auth/login` | 200 |
| GET | `/api/auth/me` | 200 |
| GET | `/api/employees?departmentId=&type=&minSalary=&maxSalary=&search=&sortBy=&sortDirection=&page=&pageSize=` | 200 |
| GET | `/api/employees/{id}` | 200 |
| POST | `/api/employees` | 201 |
| PUT | `/api/employees/{id}` | 204 |
| PATCH | `/api/employees/{id}` | 200 |
| DELETE | `/api/employees/{id}` | 204 |
| POST | `/api/employees/{id}/raise` | 200 |
| GET | `/api/departments`, `/api/departments/{id}`, `/api/departments/summary` | 200 |
| POST / DELETE | `/api/departments`, `/api/departments/{id}` | 201 / 204 |

Errors are always ProblemDetails JSON with `traceId` and `correlationId`. Full status-code table:
[docs/week2/day06-rest-api.md](docs/week2/day06-rest-api.md).

### 6. Tests

```bash
dotnet test
```

- **Unit tests:** services with fake repositories, DTO validation, filtering, sorting and paging,
  the raise approval rule, middleware, JWT creation, and exception mapping.
- **Integration tests:** the whole app runs in memory (`WebApplicationFactory`) against a private
  in-memory SQLite database. They cover real HTTP status codes, authorization (401/403), persistence,
  paging in SQL, and ProblemDetails bodies.

Result: [docs/evidence/week2-build-test.log](docs/evidence/week2-build-test.log).

### Week 2 deliverables

| Day | Deliverable | Where |
|---|---|---|
| 6 | Runnable Web API with CRUD endpoints | `src/EmployeeManagement.Api` |
| 6 | `.http` file with success and failure requests | [EmployeeManagement.Api.http](src/EmployeeManagement.Api/EmployeeManagement.Api.http) |
| 6 | OpenAPI page and saved spec; status codes | `/swagger`, [openapi-v1.json](docs/evidence/openapi-v1.json), [day06-rest-api.md](docs/week2/day06-rest-api.md) |
| 7 | DI registrations (singleton / scoped / transient) | `Program.cs`, `Application/DependencyInjection.cs`, `Infrastructure/DependencyInjection.cs` |
| 7 | Custom middleware that logs safely | [RequestLoggingMiddleware.cs](src/EmployeeManagement.Api/Middleware/RequestLoggingMiddleware.cs), [server log](docs/evidence/week2-server-log.txt) |
| 7 | Pipeline diagram and service lifetimes | [day07-di-middleware.md](docs/week2/day07-di-middleware.md) |
| 7 | Per-environment configuration | `appsettings.json`, `appsettings.Development.json`, user-secrets |
| 8 | API / Application / Infrastructure split | `src/` projects |
| 8 | DTOs, validation, consistent error examples, architecture diagram | [day08-architecture.md](docs/week2/day08-architecture.md) |
| 9 | Database-backed API with migration instructions | [day09-ef-core.md](docs/week2/day09-ef-core.md), `Infrastructure/Persistence/Migrations` |
| 9 | Relationship, filtering, sorting and paging | `Employees → Departments` FK; `GET /api/employees?...` |
| 9 | Generated SQL and tracking behaviour | [week2-ef-sql.log](docs/evidence/week2-ef-sql.log), [day09-ef-core.md](docs/week2/day09-ef-core.md) |
| 10 | Protected endpoints: success, 401, 403 demo | [week2-api-demo.log](docs/evidence/week2-api-demo.log), `AuthorizationTests.cs` |
| 10 | Authentication vs authorization, 401 vs 403, secrets | [day10-security.md](docs/week2/day10-security.md) |
| 6–10 | Unit tests for every new piece of logic | `tests/EmployeeManagement.Api.Tests` |

### Known limitations

- **SQLite.** Good for local development and the demo, but not for many concurrent writers. Moving to
  SQL Server or PostgreSQL means changing `UseSqlite` and the connection string, then re-generating the
  migration (the `SalaryCents` value converter could become `HasPrecision(18, 2)`).
- **Authentication is deliberately minimal.**
  - Three seeded demo users; no sign-up or user-management endpoints.
  - No refresh tokens: sign in again after 60 minutes.
  - No token revocation and no login rate limiting.
  - In a real system an identity provider (e.g. Microsoft Entra ID) would issue the tokens.
- **No optimistic concurrency.** Two simultaneous edits to the same employee: the last write wins
  (a concurrency token / rowversion would detect it).
- **`GET /api/departments/summary` loads all employees** into memory to reuse the Week 1 LINQ query.
  Fine for hundreds of rows; for large data it should be a SQL `GROUP BY`.
- **Fixed employee data.** An employee's type, hire date and contract end date can't be changed after
  creation.
- **HTTP in Development.** HTTPS redirection and HSTS are on only outside Development and Testing.
- **No API versioning yet.** Routes are `/api/...` without a version segment.
