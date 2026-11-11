using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Common.Models;

public sealed record RefundCashPaymentRequest(
    string PaymentVersion,
    string Reason)
{
    public override string ToString() =>
        nameof(RefundCashPaymentRequest);
}
