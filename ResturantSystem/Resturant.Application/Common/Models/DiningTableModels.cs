using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Resturant.Domain.Enums;

namespace Restaurant.Application.Common.Models;

public sealed record CreateDiningTableRequest(
    int Number,
    int Capacity);

public sealed record DiningTableResponse(
    Guid Id,
    int Number,
    int Capacity,
    string Status,
    bool HasActiveOrder,
    bool CanAcceptOrder,
    string Version);

public sealed record DiningTableListFilter(
    int Page,
    int PageSize,
    int? Number,
    int? MinimumCapacity,
    TableStatus? Status,
    bool? CanAcceptOrder,
    string SortBy,
    bool Descending);
