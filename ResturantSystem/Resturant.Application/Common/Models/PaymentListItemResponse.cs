using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Common.Models;

public sealed record PaymentListItemResponse(
    Guid Id,
    Guid OrderId,
    decimal Amount,
    string Currency,
    string Method,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? PaidAtUtc,
    DateTimeOffset? RefundedAtUtc,
    string Version);
