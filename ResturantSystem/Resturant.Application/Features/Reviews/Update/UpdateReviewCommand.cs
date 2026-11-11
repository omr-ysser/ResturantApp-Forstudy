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

namespace Restaurant.Application.Features.Reviews.Update;

public sealed record UpdateReviewCommand(
    Guid Id,
    int Rating,
    string? Comment,
    string Version)
    : IRequest<ReviewResponse>
{
    public override string ToString() =>
        nameof(UpdateReviewCommand);
}

public sealed class UpdateReviewCommandValidator
    : AbstractValidator<UpdateReviewCommand>
{
    public UpdateReviewCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Rating)
            .InclusiveBetween(
                ReviewRules.MinimumRating,
                ReviewRules.MaximumRating);

        RuleFor(command => command.Comment)
            .MaximumLength(
                ReviewRules.MaximumCommentLength);

        RuleFor(command => command.Version)
            .Must(RowVersionFormat.IsValid)
            .WithMessage(
                "Version must be a valid Base64 SQL Server rowversion.");
    }
}

public sealed class UpdateReviewCommandHandler(
    IReviewService reviewService)
    : IRequestHandler<
        UpdateReviewCommand,
        ReviewResponse>
{
    public Task<ReviewResponse> Handle(
        UpdateReviewCommand request,
        CancellationToken cancellationToken)
    {
        return reviewService.UpdateAsync(
            request.Id,
            request.Rating,
            request.Comment,
            request.Version,
            cancellationToken);
    }
}
