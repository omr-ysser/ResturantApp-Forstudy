using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Resturant.Domain.Common;
using Resturant.Domain.Enums;
using Resturant.Domain.Exceptions;

namespace Resturant.Domain.Entities;

public sealed class Review : BaseEntity
{
    private Review()
    {
    }

    public Review(
        Order order,
        Guid authorUserId,
        int rating,
        string? comment)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (authorUserId == Guid.Empty)
        {
            throw new DomainException(
                "Review author is required.");
        }

        if (order.Status != OrderStatus.Completed)
        {
            throw new DomainException(
                "Only completed orders can be reviewed.");
        }

        if (!order.CustomerUserId.HasValue ||
            order.CustomerUserId.Value != authorUserId)
        {
            throw new DomainException(
                "Only the customer who owns the order can review it.");
        }

        ValidateRating(rating);

        var normalizedComment = NormalizeComment(comment);

        OrderId = order.Id;
        AuthorUserId = authorUserId;
        Rating = rating;
        Comment = normalizedComment;
    }

    public Guid OrderId { get; private set; }

    public Order Order { get; private set; } = null!;

    public Guid AuthorUserId { get; private set; }

    public User AuthorUser { get; private set; } = null!;

    public int Rating { get; private set; }

    public string? Comment { get; private set; }

    public bool IsHidden { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public Guid? LastModeratedByUserId { get; private set; }

    public User? LastModeratedByUser { get; private set; }

    public DateTimeOffset? LastModeratedAtUtc { get; private set; }

    public string? LastModerationReason { get; private set; }

    public void UpdateContent(
        Guid actorUserId,
        int rating,
        string? comment)
    {
        EnsureAuthor(actorUserId);

        EnsureNotDeleted();

        ValidateRating(rating);

        var normalizedComment = NormalizeComment(comment);

        Rating = rating;
        Comment = normalizedComment;

        // Editing content does not remove a moderation decision.
        MarkUpdated();
    }

    public void Delete(
        Guid actorUserId,
        DateTimeOffset deletedAtUtc)
    {
        EnsureAuthor(actorUserId);

        EnsureNotDeleted();

        var normalizedTime = ValidateTimestamp(
            deletedAtUtc,
            "Deletion");

        IsDeleted = true;
        DeletedAtUtc = normalizedTime;

        // Withdraw the author's comment while keeping
        // the order-review uniqueness record.
        Comment = null;

        MarkUpdated();
    }

    public void SetHidden(
        bool isHidden,
        Guid moderatorUserId,
        string reason,
        DateTimeOffset occurredAtUtc)
    {
        EnsureNotDeleted();

        if (moderatorUserId == Guid.Empty)
        {
            throw new DomainException(
                "Review moderator is required.");
        }

        if (IsHidden == isHidden)
        {
            throw new DomainException(
                "The review already has the requested visibility.");
        }

        var normalizedReason = OrderRules.RequiredText(
            reason,
            ReviewRules.MaximumModerationReasonLength,
            "Moderation reason");

        var normalizedTime = ValidateTimestamp(
            occurredAtUtc,
            "Moderation");

        if (LastModeratedAtUtc.HasValue &&
            normalizedTime < LastModeratedAtUtc.Value)
        {
            throw new DomainException(
                "Moderation timestamp cannot precede the previous action.");
        }

        IsHidden = isHidden;
        LastModeratedByUserId = moderatorUserId;
        LastModeratedAtUtc = normalizedTime;
        LastModerationReason = normalizedReason;

        MarkUpdated();
    }

    private void EnsureAuthor(Guid actorUserId)
    {
        if (actorUserId == Guid.Empty ||
            actorUserId != AuthorUserId)
        {
            throw new DomainException(
                "Only the review author can perform this operation.");
        }
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new DomainException(
                "The review has been deleted.");
        }
    }

    private DateTimeOffset ValidateTimestamp(
        DateTimeOffset timestamp,
        string operation)
    {
        if (timestamp == default)
        {
            throw new DomainException(
                $"{operation} timestamp is required.");
        }

        var utc = timestamp.ToUniversalTime();

        if (utc < CreatedAtUtc)
        {
            throw new DomainException(
                $"{operation} timestamp cannot precede review creation.");
        }

        return utc;
    }

    private static void ValidateRating(int rating)
    {
        if (rating < ReviewRules.MinimumRating ||
            rating > ReviewRules.MaximumRating)
        {
            throw new DomainException(
                $"Rating must be between " +
                $"{ReviewRules.MinimumRating} and " +
                $"{ReviewRules.MaximumRating}.");
        }
    }

    private static string? NormalizeComment(string? comment)
    {
        return OrderRules.OptionalText(
            comment,
            ReviewRules.MaximumCommentLength,
            "Review comment");
    }
}
