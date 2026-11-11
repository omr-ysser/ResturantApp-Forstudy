using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Features.Authentication.ChangePassword;
using Resturant.Api.Authentication;

namespace Resturant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/auth")]
[ServiceFilter(typeof(AuthRequestGuard))]
public sealed class PasswordController(
    ISender sender,
    RefreshCookie refreshCookie)
    : ControllerBase
{
    [HttpPost("change-password")]
    [EnableRateLimiting("login")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
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
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        Response.Headers["Cache-Control"] = "no-store";
        Response.Headers["Pragma"] = "no-cache";

        await sender.Send(
            new ChangePasswordCommand(
                request.CurrentPassword,
                request.NewPassword),
            cancellationToken);

        refreshCookie.Delete(Response);

        return NoContent();
    }
}
