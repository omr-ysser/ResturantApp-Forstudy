using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Features.Reservations.List;
using Resturant.Domain.Enums;

namespace Resturant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reservations")]
[ResponseCache(
    NoStore = true,
    Location = ResponseCacheLocation.None)]
public sealed class ReservationListsController(
    ISender sender)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(
        typeof(PagedResponse<ReservationResponse>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<
        PagedResponse<ReservationResponse>>> List(
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] ReservationStatus? status = null,
        [FromQuery] Guid? diningTableId = null,
        [FromQuery] DateTimeOffset? fromUtc = null,
        [FromQuery] DateTimeOffset? toUtc = null,
        [FromQuery] bool mineOnly = false,
        [FromQuery] string sortBy = "startsAt",
        [FromQuery] bool descending = false)
    {
        // The handler enforces ViewAll OR ViewOwn plus data scope.
        var response = await sender.Send(
            new ListReservationsQuery(
                page,
                pageSize,
                search,
                status,
                diningTableId,
                fromUtc,
                toUtc,
                mineOnly,
                sortBy,
                descending),
            cancellationToken);

        return Ok(response);
    }
}
