using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using MediatR;
using Restaurant.Application.Abstractions.Services;
using Restaurant.Application.Common.Security;

namespace Restaurant.Application.Features.Reviews.Delete;

public sealed record DeleteReviewCommand(
    Guid Id,
    string Version)
    : IRequest<Unit>;

public sealed class DeleteReviewCommandValidator
    : AbstractValidator<DeleteReviewCommand>
{
    public DeleteReviewCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Version)
            .Must(RowVersionFormat.IsValid)
            .WithMessage(
                "Version must be a valid Base64 SQL Server rowversion.");
    }
}

public sealed class DeleteReviewCommandHandler(
    IReviewService reviewService)
    : IRequestHandler<DeleteReviewCommand, Unit>
{
    public async Task<Unit> Handle(
        DeleteReviewCommand request,
        CancellationToken cancellationToken)
    {
        await reviewService.DeleteAsync(
            request.Id,
            request.Version,
            cancellationToken);

        return Unit.Value;
    }
}
