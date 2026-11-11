using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Features.Reviews;
using Restaurant.Application.Features.Reviews.List;
using Restaurant.Application.Features.Reviews.Summary;
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
public sealed class ReviewQueriesController(
    ISender sender)
    : ControllerBase
{
    [HttpGet]
    [RequirePermission(ReviewPermissions.View)]
    [ProducesResponseType(
        typeof(PagedResponse<ReviewResponse>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<
        PagedResponse<ReviewResponse>>> List(
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] int? rating = null,
        [FromQuery] bool mineOnly = false,
        [FromQuery] ReviewVisibility visibility =
            ReviewVisibility.Visible,
        [FromQuery] string sortBy = "createdAt",
        [FromQuery] bool descending = true)
    {
        var response = await sender.Send(
            new ListReviewsQuery(
                page,
                pageSize,
                search,
                rating,
                mineOnly,
                visibility,
                sortBy,
                descending),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("summary")]
    [RequirePermission(ReviewPermissions.View)]
    [ProducesResponseType(
        typeof(ReviewSummaryResponse),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<ReviewSummaryResponse>> Summary(
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new GetReviewSummaryQuery(),
            cancellationToken);

        return Ok(response);
    }
}
