using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Features.Clients;
using Restaurant.Application.Features.Clients.CreateMyProfile;
using Restaurant.Application.Features.Clients.GetMyProfile;
using Restaurant.Application.Features.Clients.UpdateMyProfile;
using Resturant.Api.Authorization;

namespace Resturant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/clients/me")]
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
public sealed class MyClientProfileController(
    ISender sender)
    : ControllerBase
{
    [HttpGet]
    [RequirePermission(ClientPermissions.ViewOwn)]
    [ProducesResponseType(
        typeof(CustomerProfileResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerProfileResponse>> Get(
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new GetMyCustomerProfileQuery(),
            cancellationToken);

        return Ok(response);
    }

    [HttpPost]
    [RequirePermission(ClientPermissions.CreateOwn)]
    [ProducesResponseType(
        typeof(CustomerProfileResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomerProfileResponse>> Create(
        [FromBody] CreateMyCustomerProfileRequest request,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new CreateMyCustomerProfileCommand(
                request.PhoneNumber),
            cancellationToken);

        return CreatedAtAction(
            nameof(Get),
            response);
    }

    [HttpPut]
    [RequirePermission(ClientPermissions.EditOwn)]
    [ProducesResponseType(
        typeof(CustomerProfileResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CustomerProfileResponse>> Update(
        [FromBody] UpdateMyCustomerProfileRequest request,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new UpdateMyCustomerProfileCommand(
                request.PhoneNumber,
                request.Version),
            cancellationToken);

        return Ok(response);
    }
}
