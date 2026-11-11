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

public sealed class CustomerProfileConfiguration
    : BaseEntityConfiguration<CustomerProfile>
{
    protected override void ConfigureEntity(
        EntityTypeBuilder<CustomerProfile> builder)
    {
        builder.ToTable("CustomerProfiles");

        builder.Property(profile => profile.PhoneNumber)
            .HasMaxLength(CustomerRules.MaximumPhoneLength)
            .IsRequired();

        builder.HasIndex(profile => profile.UserId)
            .IsUnique()
            .HasDatabaseName(
                "UX_CustomerProfiles_UserId");

        builder.HasOne(profile => profile.User)
            .WithOne()
            .HasForeignKey<CustomerProfile>(
                profile => profile.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(profile => profile.DeliveryAddresses)
            .WithOne(address => address.CustomerProfile)
            .HasForeignKey(address =>
                address.CustomerProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(profile => profile.DeliveryAddresses)
            .HasField("_deliveryAddresses")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
