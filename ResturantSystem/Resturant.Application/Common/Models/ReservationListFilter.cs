using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Resturant.Domain.Enums;

namespace Restaurant.Application.Common.Models;

public sealed record ReservationListFilter(
    int Page,
    int PageSize,
    string? Search,
    ReservationStatus? Status,
    Guid? DiningTableId,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc,
    string SortBy,
    bool Descending);
