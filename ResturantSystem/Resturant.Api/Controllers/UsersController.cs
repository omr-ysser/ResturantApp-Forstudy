using MediatR;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Features.Users.Create;
using Restaurant.Application.Features.Users.Get;
using Restaurant.Application.Features.Users.List;
using Restaurant.Application.Features.Users.SetRoles;
using Restaurant.Application.Features.Users.SetStatus;
using Restaurant.Application.Features.Users.UpdateProfile;
using Resturant.Api.Authorization;

namespace Resturant.Api.Controllers;

[ApiController]
[Route("api/users")]
[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status400BadRequest)]
[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status401Unauthorized)]
[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status403Forbidden)]
public sealed class UsersController(
    ISender sender)
    : ControllerBase
{
    [HttpGet]
    [RequirePermission("Users.View")]
    [ProducesResponseType(
        typeof(PagedResponse<UserSummaryResponse>),
        StatusCodes.Status200OK)]
    public async Task<
        ActionResult<PagedResponse<UserSummaryResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        SetNoStore();

        var response = await sender.Send(
            new ListUsersQuery(
                page,
                pageSize,
                search,
                isActive),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("Users.View")]
    [ProducesResponseType(
        typeof(UserAccessResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserAccessResponse>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        SetNoStore();

        var response = await sender.Send(
            new GetUserQuery(id),
            cancellationToken);

        return Ok(response);
    }

    [HttpPost]
    [RequirePermission("Users.Create")]
    [ProducesResponseType(
        typeof(UserAccessResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserAccessResponse>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        SetNoStore();

        var response = await sender.Send(
            new CreateUserCommand(
                request.FullName,
                request.Email,
                request.Password,
                request.RoleIds),
            cancellationToken);

        return CreatedAtAction(
            nameof(Get),
            new { id = response.Id },
            response);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("Users.Edit")]
    [ProducesResponseType(
        typeof(UserAccessResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserAccessResponse>>
        UpdateProfile(
            Guid id,
            [FromBody] UpdateUserProfileRequest request,
            CancellationToken cancellationToken)
    {
        SetNoStore();

        var response = await sender.Send(
            new UpdateUserProfileCommand(
                id,
                request.FullName,
                request.Email,
                request.Version),
            cancellationToken);

        return Ok(response);
    }

    [HttpPut("{id:guid}/roles")]
    [RequirePermission("Users.Edit")]
    [ProducesResponseType(
        typeof(UserAccessResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserAccessResponse>> SetRoles(
        Guid id,
        [FromBody] SetUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        SetNoStore();

        var response = await sender.Send(
            new SetUserRolesCommand(
                id,
                request.Version,
                request.RoleIds),
            cancellationToken);

        return Ok(response);
    }

    [HttpPut("{id:guid}/status")]
    [RequirePermission("Users.Edit")]
    [ProducesResponseType(
        typeof(UserAccessResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserAccessResponse>> SetStatus(
        Guid id,
        [FromBody] SetUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        SetNoStore();

        var response = await sender.Send(
            new SetUserStatusCommand(
                id,
                request.Version,
                request.IsActive),
            cancellationToken);

        return Ok(response);
    }

    private void SetNoStore()
    {
        Response.Headers["Cache-Control"] = "no-store";
    }
}