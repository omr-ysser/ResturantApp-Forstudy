using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Features.Payments;
using Restaurant.Application.Features.Payments.RefundCash;
using Resturant.Api.Authorization;

namespace Resturant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/orders/{orderId:guid}/payments/{paymentId:guid}")]
[ResponseCache(
    NoStore = true,
    Location = ResponseCacheLocation.None)]
public sealed class CashRefundsController(
    ISender sender)
    : ControllerBase
{
    [HttpPost("refund-cash")]
    [RequirePermission(PaymentPermissions.RefundCash)]
    [ProducesResponseType(
        typeof(CashPaymentResponse),
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
    public async Task<ActionResult<CashPaymentResponse>> Refund(
        Guid orderId,
        Guid paymentId,
        [FromBody] RefundCashPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new RefundCashPaymentCommand(
                orderId,
                paymentId,
                request.PaymentVersion,
                request.Reason),
            cancellationToken);

        return Ok(response);
    }
}
