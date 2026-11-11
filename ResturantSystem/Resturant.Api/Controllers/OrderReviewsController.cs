using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Features.Reviews;
using Restaurant.Application.Features.Reviews.Create;
using Restaurant.Application.Features.Reviews.GetForOrder;
using Resturant.Api.Authorization;

namespace Resturant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/orders/{orderId:guid}/review")]
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
public sealed class OrderReviewsController(
    ISender sender)
    : ControllerBase
{
    [HttpPost]
    [RequirePermission(ReviewPermissions.CreateOwn)]
    [ProducesResponseType(
        typeof(ReviewResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReviewResponse>> Create(
        Guid orderId,
        [FromBody] CreateReviewRequest request,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new CreateReviewCommand(
                orderId,
                request.Rating,
                request.Comment),
            cancellationToken);

        return CreatedAtAction(
            nameof(Get),
            new { orderId },
            response);
    }

    [HttpGet]
    [RequirePermission(ReviewPermissions.View)]
    [ProducesResponseType(
        typeof(ReviewResponse),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ReviewResponse>> Get(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new GetOrderReviewQuery(orderId),
            cancellationToken);

        return Ok(response);
    }
}
