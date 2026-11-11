using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Features.Orders.List;
using Resturant.Domain.Enums;

namespace Resturant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/orders")]
[ResponseCache(
    NoStore = true,
    Location = ResponseCacheLocation.None)]
public sealed class OrderListsController(
    ISender sender)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(
        typeof(PagedResponse<OrderListItemResponse>),
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
        PagedResponse<OrderListItemResponse>>> List(
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] OrderStatus? status = null,
        [FromQuery] OrderType? type = null,
        [FromQuery] DateTimeOffset? fromUtc = null,
        [FromQuery] DateTimeOffset? toUtc = null,
        [FromQuery] decimal? minTotal = null,
        [FromQuery] decimal? maxTotal = null,
        [FromQuery] string sortBy = "createdAt",
        [FromQuery] bool descending = true,
        [FromQuery] bool mineOnly = false)
    {
        // The query handler enforces:
        // Orders.ViewAll OR Orders.ViewOwn plus data scope.
        var response = await sender.Send(
            new ListOrdersQuery(
                page,
                pageSize,
                search,
                status,
                type,
                fromUtc,
                toUtc,
                minTotal,
                maxTotal,
                sortBy,
                descending,
                mineOnly),
            cancellationToken);

        return Ok(response);
    }
}
