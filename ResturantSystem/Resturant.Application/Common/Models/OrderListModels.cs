using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Resturant.Domain.Enums;

namespace Restaurant.Application.Common.Models;

public sealed record OrderListFilter(
    int Page,
    int PageSize,
    string? Search,
    OrderStatus? Status,
    OrderType? Type,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc,
    decimal? MinTotal,
    decimal? MaxTotal,
    string SortBy,
    bool Descending);

public sealed record OrderListItemResponse(
    Guid Id,
    string OrderNumber,
    string Type,
    string Status,
    Guid? CustomerUserId,
    Guid? DiningTableId,
    int LineCount,
    decimal TotalAmount,
    string Currency,
    DateTimeOffset CreatedAtUtc);
