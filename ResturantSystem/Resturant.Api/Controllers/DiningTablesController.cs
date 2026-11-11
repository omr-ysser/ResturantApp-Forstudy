using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Features.DiningTables;
using Restaurant.Application.Features.DiningTables.Create;
using Restaurant.Application.Features.DiningTables.Get;
using Restaurant.Application.Features.DiningTables.List;
using Resturant.Api.Authorization;
using Resturant.Domain.Enums;

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
public sealed class DiningTablesController(
    ISender sender)
    : ControllerBase
{
    [HttpGet]
    [RequirePermission(DiningTablePermissions.View)]
    [ProducesResponseType(
        typeof(PagedResponse<DiningTableResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<
        PagedResponse<DiningTableResponse>>> List(
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? number = null,
        [FromQuery] int? minimumCapacity = null,
        [FromQuery] TableStatus? status = null,
        [FromQuery] bool? canAcceptOrder = null,
        [FromQuery] string sortBy = "number",
        [FromQuery] bool descending = false)
    {
        var response = await sender.Send(
            new ListDiningTablesQuery(
                page,
                pageSize,
                number,
                minimumCapacity,
                status,
                canAcceptOrder,
                sortBy,
                descending),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(DiningTablePermissions.View)]
    [ProducesResponseType(
        typeof(DiningTableResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DiningTableResponse>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new GetDiningTableQuery(id),
            cancellationToken);

        return Ok(response);
    }

    [HttpPost]
    [RequirePermission(DiningTablePermissions.Create)]
    [ProducesResponseType(
        typeof(DiningTableResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DiningTableResponse>> Create(
        [FromBody] CreateDiningTableRequest request,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new CreateDiningTableCommand(
                request.Number,
                request.Capacity),
            cancellationToken);

        return CreatedAtAction(
            nameof(Get),
            new { id = response.Id },
            response);
    }
}
