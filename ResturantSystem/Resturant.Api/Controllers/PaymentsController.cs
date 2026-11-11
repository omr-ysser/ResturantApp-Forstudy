using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Features.Payments.ListForOrder;
using Resturant.Domain.Enums;

namespace Resturant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/orders/{orderId:guid}/payments")]
[ResponseCache(
    NoStore = true,
    Location = ResponseCacheLocation.None)]
public sealed class PaymentsController(
    ISender sender)
    : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(
        typeof(PagedResponse<PaymentListItemResponse>),
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
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<
        PagedResponse<PaymentListItemResponse>>> List(
        Guid orderId,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] PaymentStatus? status = null)
    {
        // The handler enforces:
        // Payments.ViewAll OR Payments.ViewOwn plus ownership.
        var response = await sender.Send(
            new ListOrderPaymentsQuery(
                orderId,
                page,
                pageSize,
                status),
            cancellationToken);

        return Ok(response);
    }
}
