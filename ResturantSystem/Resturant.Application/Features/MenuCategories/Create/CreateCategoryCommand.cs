using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using MediatR;
using Restaurant.Application.Abstractions.Services;
using Restaurant.Application.Common.Models;

namespace Restaurant.Application.Features.MenuCategories.Create;

public sealed record CreateCategoryCommand(
    string Name,
    string? Description)
    : IRequest<CategoryResponse>;

public sealed class CreateCategoryCommandValidator
    : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.Description)
            .MaximumLength(500);
    }
}

public sealed class CreateCategoryCommandHandler(
    ICategoryService categoryService)
    : IRequestHandler<
        CreateCategoryCommand,
        CategoryResponse>
{
    public Task<CategoryResponse> Handle(
        CreateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        return categoryService.CreateAsync(
            request.Name,
            request.Description,
            cancellationToken);
    }
}
