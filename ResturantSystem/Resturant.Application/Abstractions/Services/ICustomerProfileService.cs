using Restaurant.Application.Common.Models;

namespace Restaurant.Application.Abstractions.Services;

public interface ICustomerProfileService
{
    Task<CustomerProfileResponse> CreateMyProfileAsync(
        string phoneNumber,
        CancellationToken cancellationToken);

    Task<CustomerProfileResponse> UpdateMyProfileAsync(
        string phoneNumber,
        string version,
        CancellationToken cancellationToken);

    Task<DeliveryAddressChangeResponse> AddMyAddressAsync(
        DeliveryAddressInput address,
        string profileVersion,
        CancellationToken cancellationToken);

    Task<DeliveryAddressChangeResponse> UpdateMyAddressAsync(
        Guid addressId,
        DeliveryAddressInput address,
        string profileVersion,
        CancellationToken cancellationToken);

    Task<DeliveryAddressBookResponse> DeleteMyAddressAsync(
        Guid addressId,
        string profileVersion,
        CancellationToken cancellationToken);
}