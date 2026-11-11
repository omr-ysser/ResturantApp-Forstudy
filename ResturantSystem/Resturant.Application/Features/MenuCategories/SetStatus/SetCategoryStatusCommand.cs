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

namespace Restaurant.Application.Features.MenuCategories.SetStatus;

public sealed record SetCategoryStatusCommand(
    Guid Id,
    bool IsActive,
    string Version)
    : IRequest<CategoryResponse>;

public sealed class SetCategoryStatusCommandValidator
    : AbstractValidator<SetCategoryStatusCommand>
{
    public SetCategoryStatusCommandValidator()
    {
        RuleFor(request => request.Id)
            .NotEmpty();

        RuleFor(request => request.Version)
            .Must(RowVersionFormat.IsValid)
            .WithMessage(
                "Version must be a valid Base64 SQL Server rowversion.");
    }
}

public sealed class SetCategoryStatusCommandHandler(
    ICategoryService categoryService)
    : IRequestHandler<
        SetCategoryStatusCommand,
        CategoryResponse>
{
    public Task<CategoryResponse> Handle(
        SetCategoryStatusCommand request,
        CancellationToken cancellationToken)
    {
        return categoryService.SetStatusAsync(
            request.Id,
            request.IsActive,
            request.Version,
            cancellationToken);
    }
}
