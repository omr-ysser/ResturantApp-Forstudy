using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Restaurant.Application.Common.Exceptions;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Features.Authentication.Login;
using Restaurant.Application.Features.Authentication.Logout;
using Restaurant.Application.Features.Authentication.LogoutAll;
using Restaurant.Application.Features.Authentication.Refresh;
using Resturant.Api.Authentication;

namespace Resturant.Api.Controllers;

[ApiController]
[Route("api/auth")]
[ServiceFilter(typeof(AuthRequestGuard))]
public sealed class AuthController(
    ISender sender,
    RefreshCookie refreshCookie)
    : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType(
        typeof(LoginResponse),
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
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
    {
        SetNoStore();

        var result = await sender.Send(
            command,
            cancellationToken);

        refreshCookie.Write(Response, result);

        return Ok(result.Response);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("refresh")]
    [ProducesResponseType(
        typeof(LoginResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LoginResponse>> Refresh(
        CancellationToken cancellationToken)
    {
        SetNoStore();

        try
        {
            var result = await sender.Send(
                new RefreshCommand(
                    refreshCookie.Read(Request)),
                cancellationToken);

            refreshCookie.Write(Response, result);

            return Ok(result.Response);
        }
        catch (AuthenticationFailedException)
        {
            refreshCookie.Delete(Response);
            throw;
        }
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Logout(
        CancellationToken cancellationToken)
    {
        SetNoStore();

        await sender.Send(
            new LogoutCommand(),
            cancellationToken);

        refreshCookie.Delete(Response);

        return NoContent();
    }

    [HttpPost("logout-all")]
    [Authorize]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> LogoutAll(
        CancellationToken cancellationToken)
    {
        SetNoStore();

        await sender.Send(
            new LogoutAllCommand(),
            cancellationToken);

        refreshCookie.Delete(Response);

        return NoContent();
    }

    private void SetNoStore()
    {
        Response.Headers["Cache-Control"] = "no-store";
        Response.Headers["Pragma"] = "no-cache";
    }
}