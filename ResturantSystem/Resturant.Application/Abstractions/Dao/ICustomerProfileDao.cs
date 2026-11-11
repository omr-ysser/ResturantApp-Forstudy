using Restaurant.Application.Common.Models;
using Resturant.Domain.Entities;

namespace Restaurant.Application.Abstractions.Dao;

public interface ICustomerProfileDao
{
    Task<PagedResponse<ClientListItemResponse>> ListClientsAsync(
        ClientListFilter filter,
        CancellationToken cancellationToken);

    Task<ClientDetailsResponse?> GetClientDetailsAsync(
        Guid customerProfileId,
        CancellationToken cancellationToken);

    Task<CustomerProfileResponse?> GetForUserAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<DeliveryAddressBookResponse?> GetAddressBookAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<bool> ExistsForUserAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<CustomerProfile?> GetTrackedForUserAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<CustomerProfile?> GetTrackedWithAddressesForUserAsync(
        Guid userId,
        CancellationToken cancellationToken);

    byte[] GetVersion(CustomerProfile profile);

    void SetExpectedVersion(
        CustomerProfile profile,
        byte[] version);

    void Add(CustomerProfile profile);
}