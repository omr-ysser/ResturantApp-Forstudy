using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Features.Payments;
using Restaurant.Application.Features.Payments.CollectCash;
using Resturant.Api.Authorization;

namespace Resturant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/orders/{orderId:guid}/payments")]
[ResponseCache(
    NoStore = true,
    Location = ResponseCacheLocation.None)]
public sealed class CashPaymentsController(
    ISender sender)
    : ControllerBase
{
    [HttpPost("cash")]
    [RequirePermission(PaymentPermissions.CollectCash)]
    [ProducesResponseType(
        typeof(CashCollectionResponse),
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
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CashCollectionResponse>> Collect(
        Guid orderId,
        [FromHeader(Name = "Idempotency-Key")] Guid requestKey,
        [FromBody] CollectCashPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new CollectCashPaymentCommand(
                orderId,
                requestKey,
                request.OrderVersion),
            cancellationToken);

        return Ok(response);
    }
}
