using EmployeeManagement.Core.Models;

namespace EmployeeManagement.Application.Security;

/// <summary>
/// Business rule that depends on who the user is (an authorization rule beyond plain roles):
/// - HR can approve raises up to <see cref="HrMaxPercentage"/>%.
/// - Admin can approve any valid raise (up to 100%).
/// - Anyone else (Viewer) cannot approve raises.
///
/// The endpoint's [Authorize] policy already blocks Viewers; this rule adds the percentage limit,
/// which a role check alone can't express.
/// </summary>
public static class RaiseApprovalPolicy
{
    public const decimal HrMaxPercentage = 20m;
    public const decimal AdminMaxPercentage = 100m;

    /// <returns>The largest raise this user may approve, or 0 if they may not approve raises.</returns>
    public static decimal MaxApprovablePercentage(Func<string, bool> isInRole)
    {
        ArgumentNullException.ThrowIfNull(isInRole);

        if (isInRole(Roles.Admin))
            return AdminMaxPercentage;
        if (isInRole(Roles.HR))
            return HrMaxPercentage;
        return 0m;
    }

    public static bool CanApprove(decimal percentage, Func<string, bool> isInRole) =>
        percentage > 0 && percentage <= MaxApprovablePercentage(isInRole);
}
