using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Common.Models;

public sealed record ReservationActionRequest(
    string Version);

public sealed record CancelReservationRequest(
    string Reason,
    string Version)
{
    public override string ToString() =>
        nameof(CancelReservationRequest);
}

public sealed record ReservationStatusHistoryResponse(
    Guid Id,
    string? FromStatus,
    string ToStatus,
    Guid ActorUserId,
    DateTimeOffset OccurredAtUtc,
    string? Reason);