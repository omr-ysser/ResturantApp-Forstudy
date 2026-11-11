using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Features.Reservations;
using Restaurant.Application.Features.Reservations.Availability;
using Restaurant.Application.Features.Reservations.Create;
using Restaurant.Application.Features.Reservations.Get;
using Resturant.Api.Authorization;

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
public sealed class ReservationsController(
    ISender sender)
    : ControllerBase
{
    [HttpGet("availability")]
    [RequirePermission(ReservationPermissions.Create)]
    [ProducesResponseType(
        typeof(PagedResponse<AvailableReservationTableResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<
        PagedResponse<AvailableReservationTableResponse>>> Availability(
        [FromQuery] DateTimeOffset startsAtUtc,
        [FromQuery] DateTimeOffset endsAtUtc,
        [FromQuery] int guestCount,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var response = await sender.Send(
            new GetReservationAvailabilityQuery(
                startsAtUtc,
                endsAtUtc,
                guestCount,
                page,
                pageSize),
            cancellationToken);

        return Ok(response);
    }

    [HttpPost]
    [RequirePermission(ReservationPermissions.Create)]
    [ProducesResponseType(
        typeof(ReservationResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ReservationResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationResponse>> Create(
        [FromHeader(Name = "Idempotency-Key")] Guid requestKey,
        [FromBody] CreateReservationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateReservationCommand(
                requestKey,
                request),
            cancellationToken);

        if (result.Replayed)
        {
            return Ok(result.Reservation);
        }

        return CreatedAtAction(
            nameof(Get),
            new { id = result.Reservation.Id },
            result.Reservation);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(
        typeof(ReservationResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationResponse>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        // The handler enforces ViewAll OR ViewOwn plus ownership.
        var response = await sender.Send(
            new GetReservationQuery(id),
            cancellationToken);

        return Ok(response);
    }
}