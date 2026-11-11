using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Features.Orders.Cancel;
using Restaurant.Application.Features.Orders.ChangeStatus;
using Restaurant.Application.Features.Orders.History;
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
[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status404NotFound)]
public sealed class OrderActionsController(
    ISender sender)
    : ControllerBase
{
    [HttpPut("{id:guid}/status")]
    [RequirePermission("Orders.ChangeStatus")]
    [ProducesResponseType(
        typeof(OrderResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponse>> ChangeStatus(
        Guid id,
        [FromBody] ChangeOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new ChangeOrderStatusCommand(
                id,
                request.Status,
                request.Version),
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(
        typeof(OrderResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponse>> Cancel(
        Guid id,
        [FromBody] CancelOrderRequest request,
        CancellationToken cancellationToken)
    {
        // The application service enforces:
        // Orders.CancelAll OR Orders.CancelOwn plus ownership.
        var response = await sender.Send(
            new CancelOrderCommand(
                id,
                request.Reason,
                request.Version),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(
        typeof(IReadOnlyList<OrderStatusHistoryResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<
        IReadOnlyList<OrderStatusHistoryResponse>>> History(
        Guid id,
        CancellationToken cancellationToken)
    {
        // The query handler enforces:
        // Orders.ViewAll OR Orders.ViewOwn plus ownership.
        var response = await sender.Send(
            new GetOrderHistoryQuery(id),
            cancellationToken);

        return Ok(response);
    }
}
