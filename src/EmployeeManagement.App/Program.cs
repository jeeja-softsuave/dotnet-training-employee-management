using EmployeeManagement.App;
using EmployeeManagement.Core.Services;

// Composition root: build the object graph once and pass dependencies in (no "new" inside the service).
var departments = new InMemoryDepartmentRepository();
foreach (var department in SampleData.CreateDepartments())
    departments.Add(department);

var service = new EmployeeService(new InMemoryEmployeeRepository(), departments);

// Async load with cancellation: times out after 10 seconds, or the user can press Ctrl+C.
using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
{
    ConsoleCancelEventHandler onCancel = (_, e) =>
    {
        e.Cancel = true;   // don't kill the process, just cancel the load
        cts.Cancel();
    };
    Console.CancelKeyPress += onCancel;

    try
    {
        Console.WriteLine("Loading sample employees... (press Ctrl+C to cancel)");
        var result = await service.ImportAsync(new SampleEmployeeDataSource(TimeSpan.FromSeconds(1)), cts.Token);
        Console.WriteLine($"Loaded {result.Added} employees ({result.Skipped} invalid record(s) skipped).");
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Loading cancelled. Starting with an empty employee list.");
    }
    finally
    {
        Console.CancelKeyPress -= onCancel;
    }
}

// Events: subscribe after the bulk load so we only log interactive changes.
service.EmployeeAdded += (_, e) => Console.WriteLine($"[event] Hired {e.Employee.Name} (#{e.Employee.Id}).");
service.SalaryChanged += (_, e) =>
    Console.WriteLine($"[event] {e.Employee.Name}: salary {e.OldSalary:N2} -> {e.NewSalary:N2}.");

var ui = new ConsoleUi(service, Console.In, Console.Out);
ui.Run();
