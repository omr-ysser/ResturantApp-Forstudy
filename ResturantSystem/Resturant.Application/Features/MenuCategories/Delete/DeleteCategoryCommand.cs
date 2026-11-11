using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using MediatR;
using Restaurant.Application.Abstractions.Services;
using Restaurant.Application.Common.Security;

namespace Restaurant.Application.Features.MenuCategories.Delete;

public sealed record DeleteCategoryCommand(
    Guid Id,
    string Version)
    : IRequest<Unit>;

public sealed class DeleteCategoryCommandValidator
    : AbstractValidator<DeleteCategoryCommand>
{
    public DeleteCategoryCommandValidator()
    {
        RuleFor(request => request.Id)
            .NotEmpty();

        RuleFor(request => request.Version)
            .Must(RowVersionFormat.IsValid)
            .WithMessage(
                "Version must be a valid Base64 SQL Server rowversion.");
    }
}

public sealed class DeleteCategoryCommandHandler(
    ICategoryService categoryService)
    : IRequestHandler<DeleteCategoryCommand, Unit>
{
    public async Task<Unit> Handle(
        DeleteCategoryCommand request,
        CancellationToken cancellationToken)
    {
        await categoryService.DeleteAsync(
            request.Id,
            request.Version,
            cancellationToken);

        return Unit.Value;
    }
}
