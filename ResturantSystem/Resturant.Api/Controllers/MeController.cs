using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Features.Authentication.Me;

namespace Resturant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/me")]
public sealed class MeController(
    ISender sender)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(
        typeof(MeResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MeResponse>> Get(
        CancellationToken cancellationToken)
    {
        Response.Headers["Cache-Control"] = "no-store";

        var response = await sender.Send(
            new GetMeQuery(),
            cancellationToken);

        return Ok(response);
    }
}
