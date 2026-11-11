using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Resturant.Domain.Common;
using Resturant.Domain.Exceptions;
using Resturant.Domain.ValueObjects;

namespace Resturant.Domain.Entities;

public sealed class CustomerProfile : BaseEntity
{
    private readonly List<DeliveryAddress> _deliveryAddresses = [];

    private CustomerProfile()
    {
    }

    public CustomerProfile(
        Guid userId,
        string phoneNumber)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException(
                "User account is required.");
        }

        UserId = userId;

        UpdatePhoneNumber(phoneNumber);
    }

    public Guid UserId { get; private set; }

    public User User { get; private set; } = null!;

    public string PhoneNumber { get; private set; }
        = string.Empty;

    public IReadOnlyCollection<DeliveryAddress> DeliveryAddresses =>
        _deliveryAddresses.AsReadOnly();

    public void UpdatePhoneNumber(string phoneNumber)
    {
        PhoneNumber = OrderRules.RequiredText(
            phoneNumber,
            CustomerRules.MaximumPhoneLength,
            "Customer phone");

        MarkUpdated();
    }

    public DeliveryAddress AddAddress(
        DeliveryAddressDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);

        if (_deliveryAddresses.Count >=
            CustomerRules.MaximumAddresses)
        {
            throw new DomainException(
                $"A customer cannot save more than " +
                $"{CustomerRules.MaximumAddresses} delivery addresses.");
        }

        EnsureLabelIsAvailable(
            details.NormalizedLabel,
            excludingId: null);

        var address = new DeliveryAddress(
            Id,
            details);

        _deliveryAddresses.Add(address);

        MarkUpdated();

        return address;
    }

    public void UpdateAddress(
        Guid addressId,
        DeliveryAddressDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);

        var address = FindAddress(addressId);

        EnsureLabelIsAvailable(
            details.NormalizedLabel,
            address.Id);

        address.UpdateDetails(details);

        MarkUpdated();
    }

    public void RemoveAddress(Guid addressId)
    {
        var address = FindAddress(addressId);

        _deliveryAddresses.Remove(address);

        MarkUpdated();
    }

    private DeliveryAddress FindAddress(Guid addressId)
    {
        if (addressId == Guid.Empty)
        {
            throw new DomainException(
                "Delivery address identifier is required.");
        }

        return _deliveryAddresses.SingleOrDefault(
                   address => address.Id == addressId)
               ?? throw new DomainException(
                   "Delivery address was not found.");
    }

    private void EnsureLabelIsAvailable(
        string normalizedLabel,
        Guid? excludingId)
    {
        var exists = _deliveryAddresses.Any(address =>
            address.Id != excludingId &&
            string.Equals(
                address.NormalizedLabel,
                normalizedLabel,
                StringComparison.Ordinal));

        if (exists)
        {
            throw new DomainException(
                "An address with this label already exists.");
        }
    }
}
