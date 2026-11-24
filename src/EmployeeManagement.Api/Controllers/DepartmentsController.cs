using EmployeeManagement.Api.Security;
using EmployeeManagement.Application.Dtos;
using EmployeeManagement.Application.Services;
using EmployeeManagement.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Api.Controllers;

[ApiController]
[Route("api/departments")]
[Produces("application/json")]
public sealed class DepartmentsController(IDepartmentService departments) : ControllerBase
{
    /// <summary>Lists departments with headcount. Public (no sign-in needed).</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<DepartmentResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DepartmentResponse>>> List(CancellationToken cancellationToken) =>
        Ok(await departments.ListAsync(cancellationToken));

    /// <summary>Gets one department. Public.</summary>
    [HttpGet("{id:int}", Name = "GetDepartmentById")]
    [AllowAnonymous]
    [ProducesResponseType<DepartmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DepartmentResponse>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await departments.GetAsync(id, cancellationToken));

    /// <summary>Headcount and salary totals per department. Any signed-in user (salary data).</summary>
    [HttpGet("summary")]
    [ProducesResponseType<IReadOnlyList<DepartmentSummary>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DepartmentSummary>>> Summary(CancellationToken cancellationToken) =>
        Ok(await departments.GetSummaryAsync(cancellationToken));

    /// <summary>Creates a department. Admin only.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType<DepartmentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DepartmentResponse>> Create(CreateDepartmentRequest request, CancellationToken cancellationToken)
    {
        var created = await departments.CreateAsync(request, cancellationToken);
        return CreatedAtRoute("GetDepartmentById", new { id = created.Id }, created);
    }

    /// <summary>Deletes an empty department. Admin only. 409 if it still has employees.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await departments.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
