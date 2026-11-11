using EmployeeManagement.Core.Models;

namespace EmployeeManagement.Core.Services;

/// <summary>
/// Simulates a slow remote source (e.g. an HR API) with Task.Delay so cancellation can be demonstrated.
/// </summary>
public sealed class SampleEmployeeDataSource : IEmployeeDataSource
{
    private readonly TimeSpan _simulatedLatency;
    private readonly IReadOnlyList<Employee> _employees;

    public SampleEmployeeDataSource(TimeSpan simulatedLatency, IReadOnlyList<Employee>? employees = null)
    {
        if (simulatedLatency < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(simulatedLatency), "Latency cannot be negative.");

        _simulatedLatency = simulatedLatency;
        _employees = employees ?? SampleData.CreateEmployees();
    }

    public async Task<IReadOnlyList<Employee>> FetchEmployeesAsync(CancellationToken cancellationToken = default)
    {
        // Fail fast if the caller already gave up.
        cancellationToken.ThrowIfCancellationRequested();

        // Non-blocking wait: the thread is released while waiting. If the token is cancelled
        // during the wait, Task.Delay throws TaskCanceledException (an OperationCanceledException).
        await Task.Delay(_simulatedLatency, cancellationToken).ConfigureAwait(false);

        return _employees;
    }
}
