using MediatR;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Features.MenuCategories.Create;
using Restaurant.Application.Features.MenuCategories.Delete;
using Restaurant.Application.Features.MenuCategories.Get;
using Restaurant.Application.Features.MenuCategories.List;
using Restaurant.Application.Features.MenuCategories.SetStatus;
using Restaurant.Application.Features.MenuCategories.Update;
using Restaurant.Application.Features.Permissions;
using Resturant.Api.Authorization;

namespace Resturant.Api.Controllers;

[ApiController]
[Route("api/categories")]
[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status400BadRequest)]
[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status401Unauthorized)]
[ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status403Forbidden)]
public sealed class CategoriesController(
    ISender sender)
    : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionCatalog.CategoriesView)]
    [ProducesResponseType(
        typeof(PagedResponse<CategoryResponse>),
        StatusCodes.Status200OK)]
    public async Task<
        ActionResult<PagedResponse<CategoryResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string sortBy = "name",
        [FromQuery] bool descending = false,
        CancellationToken cancellationToken = default)
    {
        SetNoStore();

        var response = await sender.Send(
            new ListCategoriesQuery(
                page,
                pageSize,
                search,
                isActive,
                sortBy,
                descending),
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionCatalog.CategoriesView)]
    [ProducesResponseType(
        typeof(CategoryResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        SetNoStore();

        var response = await sender.Send(
            new GetCategoryQuery(id),
            cancellationToken);

        return Ok(response);
    }

    [HttpPost]
    [RequirePermission(PermissionCatalog.CategoriesCreate)]
    [ProducesResponseType(
        typeof(CategoryResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponse>> Create(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        SetNoStore();

        var response = await sender.Send(
            new CreateCategoryCommand(
                request.Name,
                request.Description),
            cancellationToken);

        return CreatedAtAction(
            nameof(Get),
            new { id = response.Id },
            response);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionCatalog.CategoriesEdit)]
    [ProducesResponseType(
        typeof(CategoryResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponse>> Update(
        Guid id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        SetNoStore();

        var response = await sender.Send(
            new UpdateCategoryCommand(
                id,
                request.Name,
                request.Description,
                request.Version),
            cancellationToken);

        return Ok(response);
    }

    [HttpPut("{id:guid}/status")]
    [RequirePermission(PermissionCatalog.CategoriesEdit)]
    [ProducesResponseType(
        typeof(CategoryResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponse>> SetStatus(
        Guid id,
        [FromBody] SetCategoryStatusRequest request,
        CancellationToken cancellationToken)
    {
        SetNoStore();

        var response = await sender.Send(
            new SetCategoryStatusCommand(
                id,
                request.IsActive,
                request.Version),
            cancellationToken);

        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionCatalog.CategoriesDelete)]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromQuery] string version,
        CancellationToken cancellationToken)
    {
        SetNoStore();

        await sender.Send(
            new DeleteCategoryCommand(
                id,
                version),
            cancellationToken);

        return NoContent();
    }

    private void SetNoStore()
    {
        Response.Headers["Cache-Control"] = "no-store";
    }
}
