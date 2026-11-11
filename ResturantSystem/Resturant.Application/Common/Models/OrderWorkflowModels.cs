using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Resturant.Domain.Enums;

namespace Restaurant.Application.Common.Models;

public sealed record ChangeOrderStatusRequest(
    OrderStatus Status,
    string Version);

public sealed record CancelOrderRequest(
    string Reason,
    string Version);

public sealed record OrderStatusHistoryResponse(
    Guid Id,
    string? FromStatus,
    string ToStatus,
    Guid ActorUserId,
    DateTimeOffset OccurredAtUtc,
    string? Reason);
