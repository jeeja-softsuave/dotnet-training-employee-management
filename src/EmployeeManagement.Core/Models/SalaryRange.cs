namespace EmployeeManagement.Core.Models;

/// <summary>
/// Readonly record struct: a small, immutable value type. Copied on assignment, lives inline
/// (no separate heap allocation), and compared by value. Both bounds are inclusive.
/// </summary>
public readonly record struct SalaryRange
{
    public SalaryRange(decimal min, decimal max)
    {
        if (min < 0)
            throw new ArgumentOutOfRangeException(nameof(min), min, "Minimum salary cannot be negative.");
        if (max < min)
            throw new ArgumentException("Maximum salary must be greater than or equal to minimum salary.", nameof(max));

        Min = min;
        Max = max;
    }

    public decimal Min { get; }
    public decimal Max { get; }

    // DEF-001 fix: upper bound was exclusive (salary < Max). Both bounds are inclusive by contract.
    public bool Contains(decimal salary) => salary >= Min && salary <= Max;

    public override string ToString() => $"{Min:N2} - {Max:N2}";
}
