using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Features.Dishes.Create;
using Restaurant.Application.Features.Dishes.Get;
using Restaurant.Application.Features.Dishes.List;
using Restaurant.Application.Features.Dishes.SetAvailability;
using Restaurant.Application.Features.Dishes.Update;
using Resturant.Api.Authorization;

namespace Resturant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dishes")]
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
public sealed class DishesController(
    ISender sender)
    : ControllerBase
{
    [HttpGet]
    [RequirePermission("Dishes.View")]
    [ProducesResponseType(
        typeof(PagedResponse<DishResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<DishResponse>>> List(
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] bool? isAvailable = null,
        [FromQuery] bool? isOrderable = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] string sortBy = "name",
        [FromQuery] bool descending = false)
    {
        var response = await sender.Send(
            new ListDishesQuery(
                page,
                pageSize,
                search,
                categoryId,
                isAvailable,
                isOrderable,
                minPrice,
                maxPrice,
                sortBy,
                descending),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("Dishes.View")]
    [ProducesResponseType(
        typeof(DishResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DishResponse>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new GetDishQuery(id),
            cancellationToken);

        return Ok(response);
    }

    [HttpPost]
    [RequirePermission("Dishes.Create")]
    [ProducesResponseType(
        typeof(DishResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DishResponse>> Create(
        [FromBody] CreateDishRequest request,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new CreateDishCommand(
                request.CategoryId,
                request.Name,
                request.Price,
                request.Description),
            cancellationToken);

        return CreatedAtAction(
            nameof(Get),
            new { id = response.Id },
            response);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("Dishes.Edit")]
    [ProducesResponseType(
        typeof(DishResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DishResponse>> Update(
        Guid id,
        [FromBody] UpdateDishRequest request,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new UpdateDishCommand(
                id,
                request.Name,
                request.Price,
                request.Description,
                request.Version),
            cancellationToken);

        return Ok(response);
    }

    [HttpPut("{id:guid}/availability")]
    [RequirePermission("Dishes.Edit")]
    [ProducesResponseType(
        typeof(DishResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DishResponse>> SetAvailability(
        Guid id,
        [FromBody] SetDishAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new SetDishAvailabilityCommand(
                id,
                request.IsAvailable,
                request.Version),
            cancellationToken);

        return Ok(response);
    }
}