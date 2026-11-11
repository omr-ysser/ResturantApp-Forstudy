using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Resturant.Domain.Common;

namespace Resturant.Domain.ValueObjects;

public sealed record DeliveryAddressDetails
{
    public DeliveryAddressDetails(
        string label,
        string recipientName,
        string recipientPhone,
        string city,
        string? district,
        string streetAddress,
        string? buildingDetails,
        string? deliveryInstructions)
    {
        Label = OrderRules.RequiredText(
            label,
            CustomerRules.MaximumAddressLabelLength,
            "Address label");

        NormalizedLabel = Label.ToUpperInvariant();

        RecipientName = OrderRules.RequiredText(
            recipientName,
            CustomerRules.MaximumRecipientNameLength,
            "Recipient name");

        RecipientPhone = OrderRules.RequiredText(
            recipientPhone,
            CustomerRules.MaximumPhoneLength,
            "Recipient phone");

        City = OrderRules.RequiredText(
            city,
            CustomerRules.MaximumCityLength,
            "City");

        District = OrderRules.OptionalText(
            district,
            CustomerRules.MaximumDistrictLength,
            "District");

        StreetAddress = OrderRules.RequiredText(
            streetAddress,
            CustomerRules.MaximumStreetAddressLength,
            "Street address");

        BuildingDetails = OrderRules.OptionalText(
            buildingDetails,
            CustomerRules.MaximumBuildingDetailsLength,
            "Building details");

        DeliveryInstructions = OrderRules.OptionalText(
            deliveryInstructions,
            CustomerRules.MaximumDeliveryInstructionsLength,
            "Delivery instructions");
    }

    public string Label { get; }

    public string NormalizedLabel { get; }

    public string RecipientName { get; }

    public string RecipientPhone { get; }

    public string City { get; }

    public string? District { get; }

    public string StreetAddress { get; }

    public string? BuildingDetails { get; }

    public string? DeliveryInstructions { get; }

    public override string ToString() =>
        nameof(DeliveryAddressDetails);
}
