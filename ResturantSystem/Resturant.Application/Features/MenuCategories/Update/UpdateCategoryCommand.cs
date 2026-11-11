using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using MediatR;
using Restaurant.Application.Abstractions.Services;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Common.Security;

namespace Restaurant.Application.Features.MenuCategories.Update;

public sealed record UpdateCategoryCommand(
    Guid Id,
    string Name,
    string? Description,
    string Version)
    : IRequest<CategoryResponse>;

public sealed class UpdateCategoryCommandValidator
    : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(request => request.Id)
            .NotEmpty();

        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.Description)
            .MaximumLength(500);

        RuleFor(request => request.Version)
            .Must(RowVersionFormat.IsValid)
            .WithMessage(
                "Version must be a valid Base64 SQL Server rowversion.");
    }
}

public sealed class UpdateCategoryCommandHandler(
    ICategoryService categoryService)
    : IRequestHandler<
        UpdateCategoryCommand,
        CategoryResponse>
{
    public Task<CategoryResponse> Handle(
        UpdateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        return categoryService.UpdateAsync(
            request.Id,
            request.Name,
            request.Description,
            request.Version,
            cancellationToken);
    }
}
