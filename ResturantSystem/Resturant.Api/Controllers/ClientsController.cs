using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Features.Clients;
using Restaurant.Application.Features.Clients.Get;
using Restaurant.Application.Features.Clients.List;
using Resturant.Api.Authorization;

namespace Resturant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/clients")]
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
public sealed class ClientsController(
    ISender sender)
    : ControllerBase
{
    [HttpGet]
    [RequirePermission(ClientPermissions.View)]
    [ProducesResponseType(
        typeof(PagedResponse<ClientListItemResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<
        PagedResponse<ClientListItemResponse>>> List(
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isAccountActive = null,
        [FromQuery] string sortBy = "createdAt",
        [FromQuery] bool descending = true)
    {
        var response = await sender.Send(
            new ListClientsQuery(
                page,
                pageSize,
                search,
                isAccountActive,
                sortBy,
                descending),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(ClientPermissions.View)]
    [ProducesResponseType(
        typeof(ClientDetailsResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientDetailsResponse>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new GetClientQuery(id),
            cancellationToken);

        return Ok(response);
    }
}