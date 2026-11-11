using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Features.Clients;
using Restaurant.Application.Features.Clients.Addresses;
using Resturant.Api.Authorization;

namespace Resturant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/clients/me/addresses")]
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
public sealed class MyDeliveryAddressesController(
    ISender sender)
    : ControllerBase
{
    [HttpGet]
    [RequirePermission(ClientPermissions.ViewOwn)]
    [ProducesResponseType(
        typeof(DeliveryAddressBookResponse),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<DeliveryAddressBookResponse>> List(
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new GetMyDeliveryAddressesQuery(),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(ClientPermissions.ViewOwn)]
    [ProducesResponseType(
        typeof(DeliveryAddressChangeResponse),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<DeliveryAddressChangeResponse>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new GetMyDeliveryAddressQuery(id),
            cancellationToken);

        return Ok(response);
    }

    [HttpPost]
    [RequirePermission(ClientPermissions.EditOwn)]
    [ProducesResponseType(
        typeof(DeliveryAddressChangeResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DeliveryAddressChangeResponse>> Add(
        [FromBody] AddMyDeliveryAddressRequest request,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new AddMyDeliveryAddressCommand(
                request.ProfileVersion,
                request.Address),
            cancellationToken);

        return CreatedAtAction(
            nameof(Get),
            new { id = response.Address.Id },
            response);
    }
}
