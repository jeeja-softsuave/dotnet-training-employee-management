# Day 3: LINQ queries

All queries live in `src/EmployeeManagement.Core/Queries/EmployeeQueries.cs` as extension methods on
`IEnumerable<Employee>`, and each has unit tests in `tests/.../Queries/EmployeeQueriesTests.cs`.

**Input** for every example below is the app's sample data: 10 employees (`SampleData.CreateEmployees()`
minus the invalid record) and 4 departments. *n* = number of employees, *d* = number of departments.

| # | Employee | Salary | Type | Dept | Hired | Email |
|---|---|---|---|---|---|---|
| 1 | Asha Menon | 95,000 | Full-time | 1 Engineering | 2019-03-11 | ✓ |
| 2 | Ravi Kumar | 72,000 | Full-time | 1 Engineering | 2021-07-01 | ✓ |
| 3 | Priya Nair | 60,000 | Contract (to 2027-02-14) | 1 Engineering | 2024-02-15 | — |
| 4 | Daniel Joseph | 54,000 | Full-time | 2 HR | 2018-09-03 | ✓ |
| 5 | Meera Pillai | 48,000 | Full-time | 2 HR | 2023-01-09 | — |
| 6 | Arjun Das | 88,000 | Full-time | 3 Sales | 2020-05-18 | ✓ |
| 7 | Fatima Khan | 45,000 | Contract (to 2026-12-31) | 3 Sales | 2025-06-01 | ✓ |
| 8 | Vikram Rao | 160,000 | Full-time | 1 Engineering | 2015-11-23 | ✓ |
| 9 | Sneha Iyer | 66,000 | Full-time | 3 Sales | 2022-10-10 | ✓ |
| 10 | Thomas Mathew | 52,000 | Contract (open-ended) | 1 Engineering | 2025-09-01 | ✓ |

## Queries

| # | Method | LINQ operators | Example call | Output | Time | Space |
|---|---|---|---|---|---|---|
| 1 | `InDepartment` | `Where` | `InDepartment(1)` | 1, 2, 3, 8, 10 | O(n) | O(1) streaming |
| 2 | `WithSalaryIn` | `Where` + struct predicate | `WithSalaryIn(new SalaryRange(60_000, 72_000))` | 2, 3, 9 (both bounds inclusive) | O(n) | O(1) |
| 3 | `SearchByName` | `Where` + `string.Contains(…, OrdinalIgnoreCase)` | `SearchByName("an")` | 4 Daniel Joseph, 7 Fatima Khan | O(n·m), m = name length | O(1) |
| 4 | `SortedBySalaryDescending` | `OrderByDescending`, `ThenBy` | — | 8, 1, 6, 2, 9, 3, 4, 10, 5, 7 | O(n log n) | O(n) |
| 5 | `TopEarners` | sort + `Take` | `TopEarners(3)` | Vikram, Asha, Arjun | O(n log n) | O(n) |
| 6 | `HiredOnOrAfter` | `Where`, `OrderBy` | `HiredOnOrAfter(2024-01-01)` | Priya, Fatima, Thomas | O(n log n) | O(k) |
| 7 | `SummarizeByDepartment` | `GroupBy`, `ToDictionary`, `Select`, `Sum`, `Count` | with 4 departments | Engineering 5 / 4,39,000 / avg 87,800; Sales 3 / 1,99,000 / 66,333.33; HR 2 / 1,02,000 / 51,000; Finance 0 | O(n + d log d) | O(n + d) |
| 8 | `WithDepartmentNames` | `Join` (inner join) | with 4 departments | 10 `EmployeeDetails`, e.g. (1, Asha Menon, Engineering, FullTime, 95,000) | O(n + d) (hash join) | O(d) |
| 9 | `HeadcountByType` | `GroupBy`, `ToDictionary` | — | { FullTime: 7, Contract: 3 } | O(n) | O(t), t = types |
| 10 | `MissingEmail` | `Where` | — | 3 Priya Nair, 5 Meera Pillai | O(n) | O(1) |
| 11 | `Page` | `OrderBy`, `Skip`, `Take` | `Page(2, 4)` | 5, 6, 7, 8 | O(n log n) | O(n) |
| 12 | `TotalAnnualCost` | `Sum` over a virtual method | — | 8,72,300.00 (583,000 × 1.2 + 157,000 × 1.1) | O(n) | O(1) |
| 13 | `ContractsEndingOnOrBefore` | `OfType<T>`, property pattern, `OrderBy` | `(2026-12-31)` | 7 Fatima Khan | O(n log n) | O(k) |

"Streaming" means `Where` yields one item at a time without buffering. `OrderBy` must buffer the whole
input before it can yield the first element, which is why the sorting queries use O(n) space.

## Deferred vs immediate execution

- **Deferred** (returns `IEnumerable<T>`): queries 1–6, 8, 10, 11 and 13. Nothing runs until the
  result is enumerated, and the query re-runs every time it's enumerated.
  - Test: `InDepartment_ItemAddedAfterQueryDefined_IsIncluded`. The query is defined, an employee is
    added to the list, and then enumerating the query includes the new employee.
- **Immediate**: queries 7 (`ToList`), 9 (`ToDictionary`) and 12 (`Sum`) run at once.
  - Test: `InDepartment_MaterializedWithToList_DoesNotSeeLaterChanges`. Calling `.ToList()` takes a
    snapshot, so later changes to the list don't affect it.

## Other Day 3 topics in the code

- **Generics:** `Dictionary<int, Employee>`, `IReadOnlyList<T>`, `EventHandler<TEventArgs>`
- **Lambdas and delegates:** every LINQ predicate, `Func<…>` key selectors, and `ConsoleCancelEventHandler` in `Program.cs`
- **Events:** `EmployeeService.EmployeeAdded` and `SalaryChanged`, subscribed in `Program.cs`
- **Extension methods:** all of `EmployeeQueries`, so they chain: `employees.TopEarners(3).WithDepartmentNames(depts)`
- **Pattern matching:** `EmployeeFormatter.Describe` (type and property patterns), `SalaryBand`
  (relational patterns), `ContractEndDate is { } end`
- **List vs Dictionary:** the repository uses `Dictionary` for O(1) lookup by id. A `List` would need
  an O(n) scan
