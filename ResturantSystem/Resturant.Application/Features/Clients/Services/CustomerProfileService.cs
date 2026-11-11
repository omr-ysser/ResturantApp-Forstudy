using Microsoft.Extensions.Logging;
using Restaurant.Application.Abstractions.Authentication;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Abstractions.Persistence;
using Restaurant.Application.Abstractions.Services;
using Restaurant.Application.Common.Exceptions;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Common.Security;
using Restaurant.Application.Features.Clients;
using Resturant.Domain.Common;
using Resturant.Domain.Entities;
using Resturant.Domain.ValueObjects;

namespace Restaurant.Application.Features.Clients.Services;

public sealed class CustomerProfileService(
    ICustomerProfileDao customerProfileDao,
    IAuthenticationDao authenticationDao,
    IAuthenticationSessionDao sessionDao,
    IAppDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    ILogger<CustomerProfileService> logger)
    : ICustomerProfileService
{
    public async Task<CustomerProfileResponse> CreateMyProfileAsync(
        string phoneNumber,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await dbContext.BeginTransactionAsync(
                cancellationToken);

        var userId = await GetAuthorizedUserAsync(
            ClientPermissions.CreateOwn,
            cancellationToken);

        if (await customerProfileDao.ExistsForUserAsync(
                userId,
                cancellationToken))
        {
            throw new BusinessConflictException(
                "A customer profile already exists for this account.");
        }

        var profile = new CustomerProfile(
            userId,
            phoneNumber);

        customerProfileDao.Add(profile);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        var response = await GetRequiredProfileResponseAsync(
            userId,
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        logger.LogInformation(
            "Customer profile {CustomerProfileId} created for user {UserId}",
            profile.Id,
            userId);

        return response;
    }

    public async Task<CustomerProfileResponse> UpdateMyProfileAsync(
        string phoneNumber,
        string version,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await dbContext.BeginTransactionAsync(
                cancellationToken);

        var userId = await GetAuthorizedUserAsync(
            ClientPermissions.EditOwn,
            cancellationToken);

        var profile =
            await customerProfileDao.GetTrackedForUserAsync(
                userId,
                cancellationToken);

        if (profile is null)
        {
            throw new ResourceNotFoundException("Customer profile");
        }

        EnsureExpectedVersion(profile, version);

        profile.UpdatePhoneNumber(phoneNumber);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        var response = await GetRequiredProfileResponseAsync(
            userId,
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        logger.LogInformation(
            "Customer profile {CustomerProfileId} updated by user {UserId}",
            profile.Id,
            userId);

        return response;
    }

    public async Task<DeliveryAddressChangeResponse> AddMyAddressAsync(
        DeliveryAddressInput address,
        string profileVersion,
        CancellationToken cancellationToken)
    {
        var details = CreateAddressDetails(address);

        await using var transaction =
            await dbContext.BeginTransactionAsync(
                cancellationToken);

        var userId = await GetAuthorizedUserAsync(
            ClientPermissions.EditOwn,
            cancellationToken);

        var profile = await GetOwnedProfileWithAddressesAsync(
            userId,
            profileVersion,
            cancellationToken);

        if (profile.DeliveryAddresses.Count >=
            CustomerRules.MaximumAddresses)
        {
            throw new BusinessConflictException(
                $"A customer cannot save more than " +
                $"{CustomerRules.MaximumAddresses} delivery addresses.");
        }

        EnsureLabelAvailable(
            profile,
            details.NormalizedLabel,
            excludingId: null);

        var newAddress = profile.AddAddress(details);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        var response = await GetRequiredAddressResponseAsync(
            userId,
            newAddress.Id,
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        logger.LogInformation(
            "Delivery address {DeliveryAddressId} added to customer profile {CustomerProfileId} by user {UserId}",
            newAddress.Id,
            profile.Id,
            userId);

        return response;
    }

    public async Task<DeliveryAddressChangeResponse> UpdateMyAddressAsync(
        Guid addressId,
        DeliveryAddressInput address,
        string profileVersion,
        CancellationToken cancellationToken)
    {
        var details = CreateAddressDetails(address);

        await using var transaction =
            await dbContext.BeginTransactionAsync(
                cancellationToken);

        var userId = await GetAuthorizedUserAsync(
            ClientPermissions.EditOwn,
            cancellationToken);

        var profile = await GetOwnedProfileWithAddressesAsync(
            userId,
            profileVersion,
            cancellationToken);

        EnsureAddressExists(profile, addressId);

        EnsureLabelAvailable(
            profile,
            details.NormalizedLabel,
            addressId);

        profile.UpdateAddress(
            addressId,
            details);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        var response = await GetRequiredAddressResponseAsync(
            userId,
            addressId,
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        logger.LogInformation(
            "Delivery address {DeliveryAddressId} updated in customer profile {CustomerProfileId} by user {UserId}",
            addressId,
            profile.Id,
            userId);

        return response;
    }

    public async Task<DeliveryAddressBookResponse> DeleteMyAddressAsync(
        Guid addressId,
        string profileVersion,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await dbContext.BeginTransactionAsync(
                cancellationToken);

        var userId = await GetAuthorizedUserAsync(
            ClientPermissions.EditOwn,
            cancellationToken);

        var profile = await GetOwnedProfileWithAddressesAsync(
            userId,
            profileVersion,
            cancellationToken);

        EnsureAddressExists(profile, addressId);

        // The required tracked relationship uses Cascade.
        // Removing the child makes it an orphan to be deleted.
        profile.RemoveAddress(addressId);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        var response = await GetRequiredAddressBookAsync(
            userId,
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        logger.LogInformation(
            "Delivery address {DeliveryAddressId} removed from customer profile {CustomerProfileId} by user {UserId}",
            addressId,
            profile.Id,
            userId);

        return response;
    }

    private async Task<Guid> GetAuthorizedUserAsync(
        string requiredPermission,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        // Must run inside the workflow transaction.
        await sessionDao.LockUserAsync(
            userId,
            cancellationToken);

        var access = await authenticationDao.GetAccessAsync(
            userId,
            cancellationToken);

        if (access is null ||
            !await sessionDao.IsSessionActiveAsync(
                userId,
                currentUser.SessionId,
                timeProvider.GetUtcNow(),
                cancellationToken))
        {
            throw new AuthenticationFailedException();
        }

        if (!access.Permissions.Contains(
                requiredPermission,
                StringComparer.Ordinal))
        {
            throw new ForbiddenOperationException();
        }

        return userId;
    }

    private async Task<CustomerProfile>
        GetOwnedProfileWithAddressesAsync(
            Guid userId,
            string profileVersion,
            CancellationToken cancellationToken)
    {
        var profile =
            await customerProfileDao
                .GetTrackedWithAddressesForUserAsync(
                    userId,
                    cancellationToken);

        if (profile is null)
        {
            throw new ResourceNotFoundException("Customer profile");
        }

        EnsureExpectedVersion(
            profile,
            profileVersion);

        return profile;
    }

    private void EnsureExpectedVersion(
        CustomerProfile profile,
        string version)
    {
        if (!RowVersionFormat.IsValid(version))
        {
            throw new BusinessConflictException(
                "The customer profile version is invalid. Reload the profile.");
        }

        var expectedVersion = Convert.FromBase64String(
            version);

        var actualVersion =
            customerProfileDao.GetVersion(profile);

        if (!actualVersion.AsSpan().SequenceEqual(
                expectedVersion))
        {
            throw new BusinessConflictException(
                "The customer profile has changed. Reload it and try again.");
        }

        customerProfileDao.SetExpectedVersion(
            profile,
            expectedVersion);
    }

    private static DeliveryAddressDetails CreateAddressDetails(
        DeliveryAddressInput address)
    {
        ArgumentNullException.ThrowIfNull(address);

        return new DeliveryAddressDetails(
            address.Label,
            address.RecipientName,
            address.RecipientPhone,
            address.City,
            address.District,
            address.StreetAddress,
            address.BuildingDetails,
            address.DeliveryInstructions);
    }

    private static void EnsureAddressExists(
        CustomerProfile profile,
        Guid addressId)
    {
        if (!profile.DeliveryAddresses.Any(
                address => address.Id == addressId))
        {
            throw new ResourceNotFoundException(
                "Delivery address");
        }
    }

    private static void EnsureLabelAvailable(
        CustomerProfile profile,
        string normalizedLabel,
        Guid? excludingId)
    {
        var exists = profile.DeliveryAddresses.Any(address =>
            (!excludingId.HasValue ||
             address.Id != excludingId.Value)
            &&
            string.Equals(
                address.NormalizedLabel,
                normalizedLabel,
                StringComparison.Ordinal));

        if (exists)
        {
            throw new BusinessConflictException(
                "An address with this label already exists.");
        }
    }

    private async Task<CustomerProfileResponse>
        GetRequiredProfileResponseAsync(
            Guid userId,
            CancellationToken cancellationToken)
    {
        return await customerProfileDao.GetForUserAsync(
                   userId,
                   cancellationToken)
               ?? throw new ResourceNotFoundException(
                   "Customer profile");
    }

    private async Task<DeliveryAddressBookResponse>
        GetRequiredAddressBookAsync(
            Guid userId,
            CancellationToken cancellationToken)
    {
        return await customerProfileDao.GetAddressBookAsync(
                   userId,
                   cancellationToken)
               ?? throw new ResourceNotFoundException(
                   "Customer profile");
    }

    private async Task<DeliveryAddressChangeResponse>
        GetRequiredAddressResponseAsync(
            Guid userId,
            Guid addressId,
            CancellationToken cancellationToken)
    {
        var addressBook = await GetRequiredAddressBookAsync(
            userId,
            cancellationToken);

        var address = addressBook.Addresses
            .SingleOrDefault(item => item.Id == addressId)
            ?? throw new ResourceNotFoundException(
                "Delivery address");

        return new DeliveryAddressChangeResponse(
            address,
            addressBook.ProfileVersion);
    }
}