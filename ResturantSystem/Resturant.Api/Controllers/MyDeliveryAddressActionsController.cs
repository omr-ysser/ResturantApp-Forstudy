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
[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status409Conflict)]
public sealed class MyDeliveryAddressActionsController(
    ISender sender)
    : ControllerBase
{
    [HttpPut("{id:guid}")]
    [RequirePermission(ClientPermissions.EditOwn)]
    [ProducesResponseType(
        typeof(DeliveryAddressChangeResponse),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<DeliveryAddressChangeResponse>> Update(
        Guid id,
        [FromBody] UpdateMyDeliveryAddressRequest request,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new UpdateMyDeliveryAddressCommand(
                id,
                request.ProfileVersion,
                request.Address),
            cancellationToken);

        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(ClientPermissions.EditOwn)]
    [ProducesResponseType(
        typeof(DeliveryAddressBookResponse),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<DeliveryAddressBookResponse>> Delete(
        Guid id,
        [FromQuery] string profileVersion,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new DeleteMyDeliveryAddressCommand(
                id,
                profileVersion),
            cancellationToken);

        return Ok(response);
    }
}
