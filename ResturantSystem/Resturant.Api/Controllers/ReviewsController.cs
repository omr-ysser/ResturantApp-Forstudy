using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Features.Reviews;
using Restaurant.Application.Features.Reviews.Delete;
using Restaurant.Application.Features.Reviews.Update;
using Resturant.Api.Authorization;

namespace Resturant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/reviews")]
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
[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status409Conflict)]
public sealed class ReviewsController(
    ISender sender)
    : ControllerBase
{
    [HttpPut("{id:guid}")]
    [RequirePermission(ReviewPermissions.EditOwn)]
    [ProducesResponseType(
        typeof(ReviewResponse),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ReviewResponse>> Update(
        Guid id,
        [FromBody] UpdateReviewRequest request,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new UpdateReviewCommand(
                id,
                request.Rating,
                request.Comment,
                request.Version),
            cancellationToken);

        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(ReviewPermissions.DeleteOwn)]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromQuery] string version,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new DeleteReviewCommand(
                id,
                version),
            cancellationToken);

        return NoContent();
    }
}
