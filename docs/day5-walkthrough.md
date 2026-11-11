# Day 5: Walkthrough and defect fix

## 1. One class: `Employee`

File: `src/EmployeeManagement.Core/Models/Employee.cs`

- **Abstract base class.** You can't create a plain `Employee`; you create a `FullTimeEmployee` or a
  `ContractEmployee`. `CalculateAnnualCost()` is `abstract`, so each subclass must provide it
  (full-time: +20% benefits; contract: +10% agency fee). That's **polymorphism**: `TotalAnnualCost()`
  calls the same method on every employee and gets the right result for each type.
- **Encapsulation.**
  - `Salary` has a `private set`, so the only way to change it is `GiveRaise(percentage)`.
  - `GiveRaise` validates the percentage and rounds to 2 decimal places. The setter rejects ≤ 0 and
    anything above 10,000,000.
  - An invalid raise throws, and the salary stays unchanged (tested).
- **Validation in the constructor.** An employee can't exist in an invalid state: positive id, a name
  1–100 characters long (trimmed), a positive department id, and a hire date.
- **Testable time.** `YearsOfService(DateOnly asOf)` takes the date as a parameter instead of reading
  `DateTime.Today`, so tests are deterministic. They cover the day before, the day of, and the day
  after an anniversary.
- **Why a class and not a record?** An employee has identity (id) and mutable state (salary,
  department). Two employees with the same data are still two different people.

## 2. One LINQ query: `SummarizeByDepartment`

File: `src/EmployeeManagement.Core/Queries/EmployeeQueries.cs`

```csharp
var groups = employees
    .GroupBy(e => e.DepartmentId)
    .ToDictionary(g => g.Key, g => g.ToList());

return departments
    .Select(d =>
    {
        var members = groups.GetValueOrDefault(d.Id) ?? [];
        var total = members.Sum(e => e.Salary);
        var average = members.Count == 0 ? 0m : Math.Round(total / members.Count, 2, MidpointRounding.AwayFromZero);
        return new DepartmentSummary(d.Id, d.Name, members.Count, total, average);
    })
    .OrderByDescending(s => s.Headcount)
    .ThenBy(s => s.DepartmentName, StringComparer.OrdinalIgnoreCase)
    .ToList();
```

1. `GroupBy` buckets employees by department id in one pass. `ToDictionary` makes each bucket an
   O(1) lookup.
2. It iterates the **departments**, not the groups, so a department with no employees (Finance) still
   appears, with zeros. A plain `GroupBy` would silently drop it.
3. The average of an empty group is guarded. `Average()` on an empty sequence would throw
   `InvalidOperationException`.
4. `ToList()` runs the query immediately and returns a snapshot.

Complexity: O(n + d log d) time, O(n + d) space.

Tests: headcount, total and average for Engineering (86,666.67, rounded); the empty department
included; an empty employee list.

## 3. One async operation: `EmployeeService.ImportAsync`

Files: `Services/EmployeeService.cs`, `Services/SampleEmployeeDataSource.cs`, `App/Program.cs`

```csharp
public async Task<ImportResult> ImportAsync(IEmployeeDataSource source, CancellationToken cancellationToken = default)
{
    var fetched = await source.FetchEmployeesAsync(cancellationToken).ConfigureAwait(false);
    foreach (var employee in fetched)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // skip duplicates and unknown departments, otherwise add and raise EmployeeAdded
    }
    return new ImportResult(added, skipped);
}
```

- `await` releases the thread while `Task.Delay` (standing in for a slow HR API) runs. Nothing blocks.
  There's no `.Result` or `.Wait()`, which could deadlock.
- **Cancellation is cooperative.** The token is passed down to `Task.Delay`, which throws
  `TaskCanceledException` when cancelled. The loop also checks `ThrowIfCancellationRequested()`.
- In `Program.cs` the token comes from a `CancellationTokenSource` that times out after 10 seconds or
  is cancelled by Ctrl+C. The app catches `OperationCanceledException` and continues with an empty list.
- `ConfigureAwait(false)` is used in library code because the continuation doesn't need the
  caller's context.
- Invalid records are skipped and counted rather than failing the whole import.

Tests:
- Valid and invalid records produce the right `Added`/`Skipped` counts.
- An already-cancelled token throws, and nothing is added.
- Cancelling after 50 ms during a 30-second fetch throws, and nothing is added.
- An empty source gives 0/0. A null source throws.

## Defect DEF-001

**Report:** "Filtering salaries 50,000–80,000 does not show employees who earn exactly 80,000."

**Root cause:** `SalaryRange.Contains` used `salary >= Min && salary < Max`, which made the upper bound
exclusive. The type's documented contract (and the UI prompt "Maximum salary") says both bounds are
inclusive. The existing tests didn't catch it, because no test data sat exactly on the upper bound.
That's the boundary-value gap the regression tests close.

**Regression tests added** (written first, and failing before the fix):

| Test | Why |
|---|---|
| `SalaryRangeTests.Contains_ValueEqualToMax_ReturnsTrue` | The exact boundary that was broken |
| `SalaryRangeTests.Contains_MinEqualsMax_MatchesExactValue` | Edge case: a single-value range matched nothing |
| `EmployeeQueriesTests.WithSalaryIn_EmployeeEarningExactlyMax_IsIncluded` | The user-visible symptom, at the query level |

**Before the fix** ([evidence/day5-regression-before-fix.log](evidence/day5-regression-before-fix.log)):
```
Failed EmployeeManagement.Tests.Models.SalaryRangeTests.Contains_MinEqualsMax_MatchesExactValue
Failed EmployeeManagement.Tests.Models.SalaryRangeTests.Contains_ValueEqualToMax_ReturnsTrue
Failed EmployeeManagement.Tests.Queries.EmployeeQueriesTests.WithSalaryIn_EmployeeEarningExactlyMax_IsIncluded
Failed!  - Failed:     3, Passed:   128, Skipped:     0, Total:   131
```

**Fix:** `salary < Max` → `salary <= Max` (commit "Fix DEF-001: …").

**After the fix** ([evidence/day5-regression-after-fix.log](evidence/day5-regression-after-fix.log)):
```
Passed!  - Failed:     0, Passed:   131, Skipped:     0, Total:   131
```

## How the tests choose cases

For each method there are three groups of tests:
- **Normal:** a typical valid input.
- **Boundary:** values exactly on and just past each limit. Examples: salary 0.01 and 10,000,000;
  a name of exactly 100 characters; a 100% raise; anniversary dates; contract end dates; salary-band
  edges at 49,999.99 and 50,000; a last partial page.
- **Invalid:** null, empty or whitespace input; 0 and negative values; values just above the maximum;
  unknown ids; cancelled tokens.

Tests that depend on dates take the date as a parameter, so they never depend on today's date.
