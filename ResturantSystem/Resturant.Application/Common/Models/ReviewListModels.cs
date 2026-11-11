using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Common.Models;

public enum ReviewVisibility
{
    Visible = 1,
    Hidden = 2,
    All = 3
}

public sealed record ReviewListFilter(
    int Page,
    int PageSize,
    string? Search,
    int? Rating,
    bool MineOnly,
    ReviewVisibility Visibility,
    string SortBy,
    bool Descending);

public sealed record ReviewRatingCount(
    int Rating,
    int Count);

public sealed record ReviewSummaryResponse(
    int TotalReviews,
    decimal? AverageRating,
    IReadOnlyList<ReviewRatingCount> Distribution);
