using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Features.Orders.Create;
using Restaurant.Application.Features.Orders.Get;
using Resturant.Api.Authorization;

namespace Resturant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/orders")]
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
public sealed class OrdersController(
    ISender sender)
    : ControllerBase
{
    [HttpPost]
    [RequirePermission("Orders.Create")]
    [ProducesResponseType(
        typeof(OrderResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(OrderResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponse>> Create(
        [FromHeader(Name = "Idempotency-Key")] Guid requestKey,
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateOrderCommand(
                requestKey,
                request),
            cancellationToken);

        if (result.Replayed)
        {
            return Ok(result.Order);
        }

        return CreatedAtAction(
            nameof(Get),
            new { id = result.Order.Id },
            result.Order);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(
        typeof(OrderResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        // GetOrderQueryHandler enforces:
        // Orders.ViewAll OR Orders.ViewOwn plus ownership.
        var response = await sender.Send(
            new GetOrderQuery(id),
            cancellationToken);

        return Ok(response);
    }
}
