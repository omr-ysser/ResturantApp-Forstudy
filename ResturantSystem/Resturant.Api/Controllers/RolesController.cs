using MediatR;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Features.Roles.Create;
using Restaurant.Application.Features.Roles.Delete;
using Restaurant.Application.Features.Roles.Get;
using Restaurant.Application.Features.Roles.List;
using Restaurant.Application.Features.Roles.SetPermissions;
using Restaurant.Application.Features.Roles.Update;
using Resturant.Api.Authorization;

namespace Resturant.Api.Controllers;

[ApiController]
[Route("api/roles")]
[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status400BadRequest)]
[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status401Unauthorized)]
[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status403Forbidden)]
public sealed class RolesController(
    ISender sender)
    : ControllerBase
{
    [HttpGet]
    [RequirePermission("Roles.View")]
    [ProducesResponseType(
        typeof(PagedResponse<RoleSummaryResponse>),
        StatusCodes.Status200OK)]
    public async Task<
        ActionResult<PagedResponse<RoleSummaryResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        SetNoStore();

        var response = await sender.Send(
            new ListRolesQuery(
                page,
                pageSize,
                search),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("Roles.View")]
    [ProducesResponseType(
        typeof(RolePermissionsResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RolePermissionsResponse>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        SetNoStore();

        var response = await sender.Send(
            new GetRoleQuery(id),
            cancellationToken);

        return Ok(response);
    }

    [HttpPost]
    [RequirePermission("Roles.Create")]
    [ProducesResponseType(
        typeof(RolePermissionsResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RolePermissionsResponse>> Create(
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        SetNoStore();

        var response = await sender.Send(
            new CreateRoleCommand(request.Name),
            cancellationToken);

        return CreatedAtAction(
            nameof(Get),
            new { id = response.Id },
            response);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("Roles.Edit")]
    [ProducesResponseType(
        typeof(RolePermissionsResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RolePermissionsResponse>> Update(
        Guid id,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        SetNoStore();

        var response = await sender.Send(
            new UpdateRoleCommand(
                id,
                request.Name,
                request.Version),
            cancellationToken);

        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("Roles.Delete")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromQuery] string version,
        CancellationToken cancellationToken)
    {
        SetNoStore();

        await sender.Send(
            new DeleteRoleCommand(
                id,
                version),
            cancellationToken);

        return NoContent();
    }

    [HttpPut("{id:guid}/permissions")]
    [RequirePermission("Roles.ManagePermissions")]
    [ProducesResponseType(
        typeof(RolePermissionsResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RolePermissionsResponse>>
        SetPermissions(
            Guid id,
            [FromBody] SetRolePermissionsRequest request,
            CancellationToken cancellationToken)
    {
        SetNoStore();

        var response = await sender.Send(
            new SetRolePermissionsCommand(
                id,
                request.Version,
                request.PermissionIds),
            cancellationToken);

        return Ok(response);
    }

    private void SetNoStore()
    {
        Response.Headers["Cache-Control"] = "no-store";
    }
}