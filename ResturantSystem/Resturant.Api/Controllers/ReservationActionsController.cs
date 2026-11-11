using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Features.Reservations;
using Restaurant.Application.Features.Reservations.ChangeState;
using Restaurant.Application.Features.Reservations.History;
using Resturant.Api.Authorization;
using Resturant.Domain.Enums;

namespace Resturant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reservations")]
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
public sealed class ReservationActionsController(
    ISender sender)
    : ControllerBase
{
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(
        typeof(ReservationResponse),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ReservationResponse>> Cancel(
        Guid id,
        [FromBody] CancelReservationRequest request,
        CancellationToken cancellationToken)
    {
        // The service enforces CancelAll OR CancelOwn plus ownership.
        var response = await sender.Send(
            new ChangeReservationStateCommand(
                id,
                ReservationStatus.Cancelled,
                request.Version,
                request.Reason),
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("{id:guid}/seat")]
    [RequirePermission(ReservationPermissions.Manage)]
    [ProducesResponseType(
        typeof(ReservationResponse),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ReservationResponse>> Seat(
        Guid id,
        [FromBody] ReservationActionRequest request,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new ChangeReservationStateCommand(
                id,
                ReservationStatus.Seated,
                request.Version),
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("{id:guid}/complete")]
    [RequirePermission(ReservationPermissions.Manage)]
    [ProducesResponseType(
        typeof(ReservationResponse),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ReservationResponse>> Complete(
        Guid id,
        [FromBody] ReservationActionRequest request,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new ChangeReservationStateCommand(
                id,
                ReservationStatus.Completed,
                request.Version),
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("{id:guid}/no-show")]
    [RequirePermission(ReservationPermissions.Manage)]
    [ProducesResponseType(
        typeof(ReservationResponse),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ReservationResponse>> NoShow(
        Guid id,
        [FromBody] ReservationActionRequest request,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new ChangeReservationStateCommand(
                id,
                ReservationStatus.NoShow,
                request.Version),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(
        typeof(IReadOnlyList<ReservationStatusHistoryResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<
        IReadOnlyList<ReservationStatusHistoryResponse>>> History(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new GetReservationHistoryQuery(id),
            cancellationToken);

        return Ok(response);
    }
}
