using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Resturant.Domain.Common;
using Resturant.Domain.Entities;

namespace Resturant.Infrastructure.Persistence.Configurations;

public sealed class DeliveryAddressConfiguration
    : BaseEntityConfiguration<DeliveryAddress>
{
    protected override void ConfigureEntity(
        EntityTypeBuilder<DeliveryAddress> builder)
    {
        builder.ToTable("DeliveryAddresses");

        builder.Property(address => address.Label)
            .HasMaxLength(
                CustomerRules.MaximumAddressLabelLength)
            .IsRequired();

        builder.Property(address => address.NormalizedLabel)
            .HasMaxLength(
                CustomerRules.MaximumNormalizedAddressLabelLength)
            .IsRequired();

        builder.Property(address => address.RecipientName)
            .HasMaxLength(
                CustomerRules.MaximumRecipientNameLength)
            .IsRequired();

        builder.Property(address => address.RecipientPhone)
            .HasMaxLength(
                CustomerRules.MaximumPhoneLength)
            .IsRequired();

        builder.Property(address => address.City)
            .HasMaxLength(
                CustomerRules.MaximumCityLength)
            .IsRequired();

        builder.Property(address => address.District)
            .HasMaxLength(
                CustomerRules.MaximumDistrictLength);

        builder.Property(address => address.StreetAddress)
            .HasMaxLength(
                CustomerRules.MaximumStreetAddressLength)
            .IsRequired();

        builder.Property(address => address.BuildingDetails)
            .HasMaxLength(
                CustomerRules.MaximumBuildingDetailsLength);

        builder.Property(address => address.DeliveryInstructions)
            .HasMaxLength(
                CustomerRules.MaximumDeliveryInstructionsLength);

        builder.HasIndex(address => new
        {
            address.CustomerProfileId,
            address.NormalizedLabel
        })
            .IsUnique()
            .HasDatabaseName(
                "UX_DeliveryAddresses_Profile_Label");
    }
}
