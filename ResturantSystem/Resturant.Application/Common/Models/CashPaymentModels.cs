using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Common.Models;

public sealed record CollectCashPaymentRequest(
    string OrderVersion);

public sealed record CashPaymentResponse(
    Guid Id,
    Guid OrderId,
    decimal Amount,
    string Currency,
    string Method,
    string Status,
    DateTimeOffset? PaidAtUtc,
    Guid? CollectedByUserId,
    DateTimeOffset? RefundedAtUtc,
    string Version);

public sealed record CashCollectionResponse(
    CashPaymentResponse Payment,
    bool Replayed);
