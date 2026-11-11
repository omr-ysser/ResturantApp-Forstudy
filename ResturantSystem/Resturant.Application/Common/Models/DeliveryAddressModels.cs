using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Common.Models;

public sealed record DeliveryAddressInput(
    string Label,
    string RecipientName,
    string RecipientPhone,
    string City,
    string? District,
    string StreetAddress,
    string? BuildingDetails,
    string? DeliveryInstructions)
{
    public override string ToString() =>
        nameof(DeliveryAddressInput);
}

public sealed record AddMyDeliveryAddressRequest(
    string ProfileVersion,
    DeliveryAddressInput Address)
{
    public override string ToString() =>
        nameof(AddMyDeliveryAddressRequest);
}

public sealed record DeliveryAddressResponse(
    Guid Id,
    string Label,
    string RecipientName,
    string RecipientPhone,
    string City,
    string? District,
    string StreetAddress,
    string? BuildingDetails,
    string? DeliveryInstructions,
    string Version);

public sealed record DeliveryAddressBookResponse(
    Guid CustomerProfileId,
    string ProfileVersion,
    IReadOnlyList<DeliveryAddressResponse> Addresses);

public sealed record DeliveryAddressChangeResponse(
    DeliveryAddressResponse Address,
    string ProfileVersion);
