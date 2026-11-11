using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using MediatR;
using Restaurant.Application.Abstractions.Services;
using Restaurant.Application.Common.Models;
using Resturant.Domain.Common;

namespace Restaurant.Application.Features.Reviews.Create;

public sealed record CreateReviewCommand(
    Guid OrderId,
    int Rating,
    string? Comment)
    : IRequest<ReviewResponse>
{
    public override string ToString() =>
        nameof(CreateReviewCommand);
}

public sealed class CreateReviewCommandValidator
    : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator()
    {
        RuleFor(command => command.OrderId)
            .NotEmpty();

        RuleFor(command => command.Rating)
            .InclusiveBetween(
                ReviewRules.MinimumRating,
                ReviewRules.MaximumRating);

        RuleFor(command => command.Comment)
            .MaximumLength(
                ReviewRules.MaximumCommentLength);
    }
}

public sealed class CreateReviewCommandHandler(
    IReviewService reviewService)
    : IRequestHandler<
        CreateReviewCommand,
        ReviewResponse>
{
    public Task<ReviewResponse> Handle(
        CreateReviewCommand request,
        CancellationToken cancellationToken)
    {
        return reviewService.CreateAsync(
            request.OrderId,
            request.Rating,
            request.Comment,
            cancellationToken);
    }
}
