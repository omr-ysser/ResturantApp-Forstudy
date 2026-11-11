using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Features.DiningTables;
using Restaurant.Application.Features.DiningTables.SetServiceState;
using Restaurant.Application.Features.DiningTables.Update;
using Resturant.Api.Authorization;

namespace Resturant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dining-tables")]
[ResponseCache(
    NoStore = true,
    Location = ResponseCacheLocation.None)]
[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status400BadRequest)]
[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status401Unauthorized)]
[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status403Forbidden)]
[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status404NotFound)]
[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status409Conflict)]
public sealed class DiningTableActionsController(
    ISender sender)
    : ControllerBase
{
    [HttpPut("{id:guid}")]
    [RequirePermission(DiningTablePermissions.Edit)]
    [ProducesResponseType(
        typeof(DiningTableResponse),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<DiningTableResponse>> Update(
        Guid id,
        [FromBody] UpdateDiningTableRequest request,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new UpdateDiningTableCommand(
                id,
                request.Number,
                request.Capacity,
                request.Version),
            cancellationToken);

        return Ok(response);
    }

    [HttpPut("{id:guid}/service-state")]
    [RequirePermission(DiningTablePermissions.ManageService)]
    [ProducesResponseType(
        typeof(DiningTableResponse),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<DiningTableResponse>> SetServiceState(
        Guid id,
        [FromBody] SetDiningTableServiceStateRequest request,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new SetDiningTableServiceStateCommand(
                id,
                request.IsInService,
                request.Version),
            cancellationToken);

        return Ok(response);
    }
}
