using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Common.Models;

public sealed record UpdateMyDeliveryAddressRequest(
    string ProfileVersion,
    DeliveryAddressInput Address)
{
    public override string ToString() =>
        nameof(UpdateMyDeliveryAddressRequest);
}
