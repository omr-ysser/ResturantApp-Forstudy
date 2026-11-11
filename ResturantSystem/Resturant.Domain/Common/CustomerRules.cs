using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resturant.Domain.Common;

public static class CustomerRules
{
    public const int MaximumAddresses = 10;

    public const int MaximumPhoneLength = 30;

    public const int MaximumAddressLabelLength = 50;

    public const int MaximumNormalizedAddressLabelLength = 100;

    public const int MaximumRecipientNameLength = 150;

    public const int MaximumCityLength = 80;

    public const int MaximumDistrictLength = 80;

    public const int MaximumStreetAddressLength = 180;

    public const int MaximumBuildingDetailsLength = 80;

    public const int MaximumDeliveryInstructionsLength = 300;
}
