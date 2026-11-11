# Day 1: Value and reference types in this project

**Value type:** the variable holds the data itself. Assigning it copies the data. Examples: `int`,
`decimal`, `bool`, `DateOnly`, `enum`, `struct`.

**Reference type:** the variable holds a reference to an object on the heap. Assigning it copies the
reference, so both variables point at the same object. Examples: `class`, `record`, `string`, arrays,
`List<T>`, `Dictionary<TKey, TValue>`.

## Value types used

| Type | Where | Why a value type |
|---|---|---|
| `int` | `Employee.Id`, `DepartmentId` | Small identifiers |
| `decimal` | `Employee.Salary` | Money. `decimal` is exact in base 10, while `double` would give rounding errors like `0.1 + 0.2 != 0.3` |
| `DateOnly` | `HireDate` | A calendar date with no time-zone confusion |
| `DateOnly?` (`Nullable<DateOnly>`) | `ContractEmployee.ContractEndDate` | Nullable value type: `null` means an open-ended contract |
| `EmploymentType` (enum) | `Employee.Type` | A fixed set of named constants, stored as an `int` |
| `SalaryRange` (`readonly record struct`) | Salary filter | Small (two decimals), immutable, compared by value. No heap allocation |

## Reference types used

| Type | Where | Why a reference type |
|---|---|---|
| `Employee` and subclasses (class) | Domain model | Has identity and changing state (salary). Everyone holding a reference should see the same employee |
| `Department` (record) | Domain model | A record is a reference type with **value equality**: two `Department(1, "Sales")` instances are `Equal` |
| `string` | `Name`, `Email` | Reference type that behaves like a value: it's **immutable**, and `==` compares content |
| `string?` | `Employee.Email` | Nullable reference type: the compiler warns if we use it without checking for `null` |
| `List<Employee>`, `Dictionary<int, Employee>` | Repository, queries | Collections are always reference types |
| `string[]` | `ConsoleUi.MenuItems` | Arrays are reference types even when the elements are value types |

## Demonstrated in tests

- `EmployeeTests.GiveRaise_ThroughSecondReference_VisibleThroughFirst`: `var alias = original;` then
  `alias.GiveRaise(50)` changes `original.Salary` too, because both point at one object.
- `DepartmentTests.Equals_SameValues_AreEqual`: two separate record instances are `Equal` but not
  `ReferenceEquals`.
- `SalaryRangeTests.Copy_IsIndependentValue`: copying a struct copies the value.

## string vs StringBuilder

`string` is immutable, so `s += x` in a loop creates a new string every iteration (O(n²) copying).
`EmployeeFormatter.BuildDepartmentReport` uses `StringBuilder`, which appends into one growable buffer.

## Nullable reference types

`<Nullable>enable</Nullable>` is on in every project. `string? Email` may be null and `string Name`
may not. `Employee? GetById(int id)` tells callers they must handle "not found".
`EmployeeService.GetEmployee` turns that `null` into an `EmployeeNotFoundException`.
