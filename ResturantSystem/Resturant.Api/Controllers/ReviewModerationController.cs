using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Features.Reviews;
using Restaurant.Application.Features.Reviews.Moderate;
using Resturant.Api.Authorization;

namespace Resturant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reviews")]
[ResponseCache(
    NoStore = true,
    Location = ResponseCacheLocation.None)]
public sealed class ReviewModerationController(
    ISender sender)
    : ControllerBase
{
    [HttpPut("{id:guid}/moderation")]
    [RequirePermission(ReviewPermissions.Moderate)]
    [ProducesResponseType(
        typeof(ReviewResponse),
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
    public async Task<ActionResult<ReviewResponse>> Moderate(
        Guid id,
        [FromBody] ModerateReviewRequest request,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new ModerateReviewCommand(
                id,
                request.IsHidden,
                request.Reason,
                request.Version),
            cancellationToken);

        return Ok(response);
    }
}
