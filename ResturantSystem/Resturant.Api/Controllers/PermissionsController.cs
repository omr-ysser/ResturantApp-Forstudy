using MediatR;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Features.Permissions.Create;
using Restaurant.Application.Features.Permissions.Delete;
using Restaurant.Application.Features.Permissions.Get;
using Restaurant.Application.Features.Permissions.List;
using Restaurant.Application.Features.Permissions.Update;
using Resturant.Api.Authorization;

namespace Resturant.Api.Controllers;

[ApiController]
[Route("api/permissions")]
[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status400BadRequest)]
[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status401Unauthorized)]
[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status403Forbidden)]
public sealed class PermissionsController(
    ISender sender)
    : ControllerBase
{
    [HttpGet]
    [RequirePermission("Permissions.View")]
    [ProducesResponseType(
        typeof(PagedResponse<PermissionResponse>),
        StatusCodes.Status200OK)]
    public async Task<
        ActionResult<PagedResponse<PermissionResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        Response.Headers["Cache-Control"] = "no-store";

        var response = await sender.Send(
            new ListPermissionsQuery(
                page,
                pageSize,
                search),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("Permissions.View")]
    [ProducesResponseType(
        typeof(PermissionDetailsResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<
        ActionResult<PermissionDetailsResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        Response.Headers["Cache-Control"] = "no-store";

        var response = await sender.Send(
            new GetPermissionQuery(id),
            cancellationToken);

        return Ok(response);
    }

    [HttpPost]
    [RequirePermission("Permissions.Create")]
    [ProducesResponseType(
        typeof(PermissionDetailsResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<
        ActionResult<PermissionDetailsResponse>> Create(
        [FromBody] CreatePermissionRequest request,
        CancellationToken cancellationToken)
    {
        Response.Headers["Cache-Control"] = "no-store";

        var response = await sender.Send(
            new CreatePermissionCommand(
                request.Key,
                request.DisplayName),
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = response.Id },
            response);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("Permissions.Edit")]
    [ProducesResponseType(
        typeof(PermissionDetailsResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<
        ActionResult<PermissionDetailsResponse>> Update(
        Guid id,
        [FromBody] UpdatePermissionRequest request,
        CancellationToken cancellationToken)
    {
        Response.Headers["Cache-Control"] = "no-store";

        var response = await sender.Send(
            new UpdatePermissionCommand(
                id,
                request.DisplayName,
                request.Version),
            cancellationToken);

        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("Permissions.Delete")]
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
        Response.Headers["Cache-Control"] = "no-store";

        await sender.Send(
            new DeletePermissionCommand(
                id,
                version),
            cancellationToken);

        return NoContent();
    }
}