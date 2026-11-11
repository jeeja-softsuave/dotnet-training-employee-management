# Employee & Department Management

Capstone project for the .NET training programme (Weeks 1–3), domain **Employee & Department Management**.
Week 1 delivers an in-memory C# application: a domain model, interfaces, LINQ queries, an async import with
cancellation, and xUnit tests.

## Structure

| Project | Purpose |
|---|---|
| `src/EmployeeManagement.App` | Console application: menu, input validation, composition root |
| `src/EmployeeManagement.Core` | Domain models, repository interfaces and in-memory implementations, `EmployeeService`, LINQ queries |
| `tests/EmployeeManagement.Tests` | xUnit tests (131), all following Arrange-Act-Assert |

```
src/EmployeeManagement.Core
├── Models/        Employee (abstract), FullTimeEmployee, ContractEmployee, Department (record),
│                  SalaryRange (readonly record struct), EmploymentType (enum), report records
├── Services/      IEmployeeRepository, IDepartmentRepository, IEmployeeDataSource,
│                  InMemory* implementations, EmployeeService, SampleData
├── Queries/       EmployeeQueries — 13 LINQ extension methods
├── Formatting/    EmployeeFormatter — pattern matching, StringBuilder report
└── Exceptions/    EmployeeNotFound, DepartmentNotFound, DuplicateEmployee
```

## Prerequisites

- .NET SDK 10.0 (`dotnet --version` prints `10.0.x`)

## Build, run, test

```bash
dotnet restore
dotnet build
dotnet run --project src/EmployeeManagement.App
dotnet test
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
