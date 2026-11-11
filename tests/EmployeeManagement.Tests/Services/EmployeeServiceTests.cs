using EmployeeManagement.Core.Exceptions;
using EmployeeManagement.Core.Models;
using EmployeeManagement.Core.Services;

namespace EmployeeManagement.Tests.Services;

public class EmployeeServiceTests
{
    private readonly InMemoryEmployeeRepository _employees = new();
    private readonly InMemoryDepartmentRepository _departments = new();
    private readonly EmployeeService _service;

    public EmployeeServiceTests()
    {
        foreach (var d in TestData.Departments())
            _departments.Add(d);
        _service = new EmployeeService(_employees, _departments);
    }

    // ---------- Hire ----------

    [Fact]
    public void Hire_ValidEmployee_AddsAndRaisesEvent()
    {
        // Arrange
        Employee? raisedFor = null;
        _service.EmployeeAdded += (_, e) => raisedFor = e.Employee;
        var employee = TestData.FullTime(id: 1, departmentId: 2);

        // Act
        _service.Hire(employee);

        // Assert
        Assert.Same(employee, _service.GetEmployee(1));
        Assert.Same(employee, raisedFor);
    }

    [Fact]
    public void Hire_UnknownDepartment_ThrowsAndDoesNotAdd()
    {
        var raised = false;
        _service.EmployeeAdded += (_, _) => raised = true;

        var ex = Assert.Throws<DepartmentNotFoundException>(() => _service.Hire(TestData.FullTime(departmentId: 99)));

        Assert.Equal(99, ex.DepartmentId);
        Assert.Empty(_service.GetAllEmployees());
        Assert.False(raised);
    }

    [Fact]
    public void Hire_DuplicateId_Throws()
    {
        _service.Hire(TestData.FullTime(id: 1));

        Assert.Throws<DuplicateEmployeeException>(() => _service.Hire(TestData.FullTime(id: 1)));
    }

    [Fact]
    public void Constructor_NullRepository_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new EmployeeService(null!, _departments));
    }

    // ---------- NextEmployeeId ----------

    [Fact]
    public void NextEmployeeId_Empty_ReturnsOne()
    {
        Assert.Equal(1, _service.NextEmployeeId());
    }

    [Fact]
    public void NextEmployeeId_WithGaps_ReturnsMaxPlusOne()
    {
        _service.Hire(TestData.FullTime(id: 3));
        _service.Hire(TestData.FullTime(id: 10));

        Assert.Equal(11, _service.NextEmployeeId());
    }

    // ---------- GiveRaise / Terminate / GetEmployee ----------

    [Fact]
    public void GiveRaise_ExistingEmployee_UpdatesSalaryAndRaisesEvent()
    {
        // Arrange
        _service.Hire(TestData.FullTime(id: 1, salary: 50_000m));
        SalaryChangedEventArgs? args = null;
        _service.SalaryChanged += (_, e) => args = e;

        // Act
        _service.GiveRaise(1, 10m);

        // Assert
        Assert.NotNull(args);
        Assert.Equal(50_000m, args.OldSalary);
        Assert.Equal(55_000m, args.NewSalary);
    }

    [Fact]
    public void GiveRaise_UnknownEmployee_ThrowsEmployeeNotFound()
    {
        var ex = Assert.Throws<EmployeeNotFoundException>(() => _service.GiveRaise(404, 10m));

        Assert.Equal(404, ex.EmployeeId);
    }

    [Fact]
    public void GiveRaise_InvalidPercentage_DoesNotRaiseEvent()
    {
        _service.Hire(TestData.FullTime(id: 1));
        var raised = false;
        _service.SalaryChanged += (_, _) => raised = true;

        Assert.Throws<ArgumentOutOfRangeException>(() => _service.GiveRaise(1, 0m));
        Assert.False(raised);
    }

    [Fact]
    public void Terminate_ExistingEmployee_Removes()
    {
        _service.Hire(TestData.FullTime(id: 1));

        _service.Terminate(1);

        Assert.Throws<EmployeeNotFoundException>(() => _service.GetEmployee(1));
    }

    [Fact]
    public void Terminate_UnknownEmployee_Throws()
    {
        Assert.Throws<EmployeeNotFoundException>(() => _service.Terminate(1));
    }

    [Fact]
    public void Search_DelegatesToNameSearch()
    {
        _service.Hire(TestData.FullTime(id: 1, name: "Asha Menon"));
        _service.Hire(TestData.FullTime(id: 2, name: "Ravi Kumar"));

        var result = _service.Search("menon");

        Assert.Equal("Asha Menon", Assert.Single(result).Name);
    }

    // ---------- ImportAsync ----------

    [Fact]
    public async Task ImportAsync_ValidAndInvalidRecords_AddsValidSkipsInvalid()
    {
        // Arrange: one good, one unknown department, one duplicate id of an existing employee.
        _service.Hire(TestData.FullTime(id: 1));
        var source = new SampleEmployeeDataSource(TimeSpan.Zero,
        [
            TestData.FullTime(id: 2, departmentId: 1),
            TestData.FullTime(id: 3, departmentId: 99),
            TestData.FullTime(id: 1, departmentId: 1),
        ]);
        var addedEvents = 0;
        _service.EmployeeAdded += (_, _) => addedEvents++;

        // Act
        var result = await _service.ImportAsync(source);

        // Assert
        Assert.Equal(new ImportResult(Added: 1, Skipped: 2), result);
        Assert.Equal([1, 2], _service.GetAllEmployees().Select(e => e.Id));
        Assert.Equal(1, addedEvents);
    }

    [Fact]
    public async Task ImportAsync_SampleData_LoadsTenAndSkipsGhostRecord()
    {
        foreach (var d in SampleData.CreateDepartments().Where(d => _departments.GetById(d.Id) is null))
            _departments.Add(d);

        var result = await _service.ImportAsync(new SampleEmployeeDataSource(TimeSpan.Zero));

        Assert.Equal(new ImportResult(10, 1), result);
    }

    [Fact]
    public async Task ImportAsync_EmptySource_ReturnsZeroCounts()
    {
        var result = await _service.ImportAsync(new SampleEmployeeDataSource(TimeSpan.Zero, []));

        Assert.Equal(new ImportResult(0, 0), result);
    }

    [Fact]
    public async Task ImportAsync_AlreadyCancelledToken_ThrowsWithoutAdding()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var source = new SampleEmployeeDataSource(TimeSpan.Zero, [TestData.FullTime(id: 1)]);

        // Act + Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _service.ImportAsync(source, cts.Token));
        Assert.Empty(_service.GetAllEmployees());
    }

    [Fact]
    public async Task ImportAsync_CancelledWhileWaiting_ThrowsWithoutAdding()
    {
        // Arrange: the source would take 30s; we cancel after 50ms.
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        var source = new SampleEmployeeDataSource(TimeSpan.FromSeconds(30), [TestData.FullTime(id: 1)]);

        // Act + Assert: TaskCanceledException derives from OperationCanceledException.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _service.ImportAsync(source, cts.Token));
        Assert.Empty(_service.GetAllEmployees());
    }

    [Fact]
    public async Task ImportAsync_NullSource_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.ImportAsync(null!));
    }

    // ---------- reporting ----------

    [Fact]
    public void GetDepartmentSummaries_IncludesAllDepartments()
    {
        _service.Hire(TestData.FullTime(id: 1, departmentId: 1, salary: 100m));

        var summaries = _service.GetDepartmentSummaries();

        Assert.Equal(3, summaries.Count);
        Assert.Equal(1, summaries.Single(s => s.DepartmentId == 1).Headcount);
    }
}
