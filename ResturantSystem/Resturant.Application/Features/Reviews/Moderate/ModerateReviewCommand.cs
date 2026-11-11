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
using Resturant.Domain.Common;

namespace Restaurant.Application.Features.Reviews.Moderate;

public sealed record ModerateReviewCommand(
    Guid Id,
    bool IsHidden,
    string Reason,
    string Version)
    : IRequest<ReviewResponse>
{
    public override string ToString() =>
        nameof(ModerateReviewCommand);
}

public sealed class ModerateReviewCommandValidator
    : AbstractValidator<ModerateReviewCommand>
{
    public ModerateReviewCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Reason)
            .NotEmpty()
            .MaximumLength(
                ReviewRules.MaximumModerationReasonLength);

        RuleFor(command => command.Version)
            .Must(RowVersionFormat.IsValid)
            .WithMessage(
                "Version must be a valid Base64 SQL Server rowversion.");
    }
}

public sealed class ModerateReviewCommandHandler(
    IReviewService reviewService)
    : IRequestHandler<
        ModerateReviewCommand,
        ReviewResponse>
{
    public Task<ReviewResponse> Handle(
        ModerateReviewCommand request,
        CancellationToken cancellationToken)
    {
        return reviewService.ModerateAsync(
            request.Id,
            request.IsHidden,
            request.Reason,
            request.Version,
            cancellationToken);
    }
}
