using EmployeeManagement.Api.Security;
using EmployeeManagement.Application.Common;
using EmployeeManagement.Application.Dtos;
using EmployeeManagement.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Api.Controllers;

/// <summary>
/// HTTP layer only: routing, binding, status codes, authorization attributes.
/// No business rules here. Those are in <see cref="IEmployeeService"/>.
/// [ApiController] validates the DTOs automatically and returns 400 ValidationProblemDetails.
/// </summary>
[ApiController]
[Route("api/employees")]
[Produces("application/json")]
public sealed class EmployeesController(IEmployeeService employees) : ControllerBase
{
    /// <summary>Lists employees with optional filtering, sorting and paging. Any signed-in user.</summary>
    /// <remarks>Example: GET /api/employees?departmentId=1&amp;sortBy=salary&amp;sortDirection=desc&amp;page=1&amp;pageSize=5</remarks>
    [HttpGet]
    [ProducesResponseType<PagedResult<EmployeeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<EmployeeResponse>>> List(
        [FromQuery] EmployeeQueryParameters query, CancellationToken cancellationToken) =>
        Ok(await employees.SearchAsync(query, cancellationToken));

    /// <summary>Gets one employee. Any signed-in user.</summary>
    [HttpGet("{id:int}", Name = "GetEmployeeById")]
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeResponse>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await employees.GetAsync(id, cancellationToken));

    /// <summary>Creates an employee. HR or Admin.</summary>
    /// <response code="201">Created. The Location header points at the new employee.</response>
    [HttpPost]
    [Authorize(Policy = Policies.CanManageEmployees)]
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmployeeResponse>> Create(CreateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var created = await employees.CreateAsync(request, cancellationToken);
        return CreatedAtRoute("GetEmployeeById", new { id = created.Id }, created);
    }

    /// <summary>Replaces all editable fields of an employee (PUT = full update). HR or Admin.</summary>
    /// <response code="204">Updated. No body.</response>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Policies.CanManageEmployees)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, UpdateEmployeeRequest request, CancellationToken cancellationToken)
    {
        await employees.UpdateAsync(id, request, cancellationToken);
        return NoContent();
    }

    /// <summary>Changes only the fields sent (PATCH = partial update). HR or Admin.</summary>
    [HttpPatch("{id:int}")]
    [Authorize(Policy = Policies.CanManageEmployees)]
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EmployeeResponse>> Patch(int id, PatchEmployeeRequest request, CancellationToken cancellationToken) =>
        Ok(await employees.PatchAsync(id, request, cancellationToken));

    /// <summary>Deletes an employee. Admin only.</summary>
    /// <response code="204">Deleted. No body.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Policies.AdminOnly)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await employees.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Gives a percentage raise. HR up to 20%, Admin up to 100%.</summary>
    /// <response code="403">Signed in, but your role cannot approve this raise.</response>
    [HttpPost("{id:int}/raise")]
    [Authorize(Policy = Policies.CanManageEmployees)]
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployeeResponse>> GiveRaise(int id, RaiseRequest request, CancellationToken cancellationToken) =>
        Ok(await employees.GiveRaiseAsync(id, request, cancellationToken));
}
