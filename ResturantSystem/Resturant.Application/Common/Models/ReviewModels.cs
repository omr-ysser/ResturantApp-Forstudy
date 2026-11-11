using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Common.Models;

public sealed record CreateReviewRequest(
    int Rating,
    string? Comment)
{
    public override string ToString() =>
        nameof(CreateReviewRequest);
}

public sealed record ReviewResponse(
    Guid Id,
    Guid OrderId,
    int Rating,
    string? Comment,
    bool IsHidden,
    bool IsOwn,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    string Version);
