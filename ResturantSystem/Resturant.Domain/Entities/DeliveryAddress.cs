using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Resturant.Domain.Common;
using Resturant.Domain.Exceptions;
using Resturant.Domain.ValueObjects;

namespace Resturant.Domain.Entities;

public sealed class DeliveryAddress : BaseEntity
{
    private DeliveryAddress()
    {
    }

    internal DeliveryAddress(
        Guid customerProfileId,
        DeliveryAddressDetails details)
    {
        if (customerProfileId == Guid.Empty)
        {
            throw new DomainException(
                "Customer profile is required.");
        }

        CustomerProfileId = customerProfileId;

        UpdateDetails(details);
    }

    public Guid CustomerProfileId { get; private set; }

    public CustomerProfile CustomerProfile { get; private set; }
        = null!;

    public string Label { get; private set; } = string.Empty;

    public string NormalizedLabel { get; private set; }
        = string.Empty;

    public string RecipientName { get; private set; }
        = string.Empty;

    public string RecipientPhone { get; private set; }
        = string.Empty;

    public string City { get; private set; } = string.Empty;

    public string? District { get; private set; }

    public string StreetAddress { get; private set; }
        = string.Empty;

    public string? BuildingDetails { get; private set; }

    public string? DeliveryInstructions { get; private set; }

    internal void UpdateDetails(
        DeliveryAddressDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);

        Label = details.Label;
        NormalizedLabel = details.NormalizedLabel;
        RecipientName = details.RecipientName;
        RecipientPhone = details.RecipientPhone;
        City = details.City;
        District = details.District;
        StreetAddress = details.StreetAddress;
        BuildingDetails = details.BuildingDetails;
        DeliveryInstructions = details.DeliveryInstructions;

        MarkUpdated();
    }

    public string ToOrderAddressSnapshot()
    {
        var parts = new[]
        {
            StreetAddress,
            BuildingDetails,
            District,
            City
        };

        var snapshot = string.Join(
            ", ",
            parts.Where(part =>
                !string.IsNullOrWhiteSpace(part)));

        return OrderRules.RequiredText(
            snapshot,
            OrderRules.MaximumDeliveryAddressLength,
            "Delivery address snapshot");
    }
}
