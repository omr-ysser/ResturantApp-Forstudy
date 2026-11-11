using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Common.Models;

public sealed record ClientListFilter(
    int Page,
    int PageSize,
    string? Search,
    bool? IsAccountActive,
    string SortBy,
    bool Descending);

public sealed record ClientListItemResponse(
    Guid Id,
    Guid UserId,
    string FullName,
    string Email,
    string PhoneNumber,
    bool IsAccountActive,
    int AddressCount,
    DateTimeOffset CreatedAtUtc);

public sealed record ClientDetailsResponse(
    Guid Id,
    Guid UserId,
    string FullName,
    string Email,
    string PhoneNumber,
    bool IsAccountActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    string Version,
    IReadOnlyList<DeliveryAddressResponse> Addresses);
