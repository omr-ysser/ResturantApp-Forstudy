using Microsoft.EntityFrameworkCore;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Abstractions.Persistence;
using Restaurant.Application.Common.Models;
using Resturant.Domain.Entities;

namespace Resturant.Infrastructure.Clients;

public sealed class CustomerProfileDao(
    IAppDbContext dbContext)
    : ICustomerProfileDao
{
    public async Task<PagedResponse<ClientListItemResponse>> ListClientsAsync(
        ClientListFilter filter,
        CancellationToken cancellationToken)
    {
        var query = dbContext.CustomerProfiles
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();

            var normalizedSearch = search.ToUpperInvariant();

            query = query.Where(profile =>
                profile.User.FullName.Contains(search) ||
                profile.User.NormalizedEmail.Contains(
                    normalizedSearch) ||
                profile.PhoneNumber.Contains(search));
        }

        if (filter.IsAccountActive.HasValue)
        {
            var isActive = filter.IsAccountActive.Value;

            query = query.Where(profile =>
                profile.User.IsActive == isActive);
        }

        var totalCount = await query.CountAsync(
            cancellationToken);

        var orderedQuery = filter.SortBy
            .ToLowerInvariant() switch
        {
            "createdat" => filter.Descending
                ? query.OrderByDescending(
                    profile => profile.CreatedAtUtc)
                : query.OrderBy(
                    profile => profile.CreatedAtUtc),

            "name" => filter.Descending
                ? query.OrderByDescending(
                    profile => profile.User.FullName)
                : query.OrderBy(
                    profile => profile.User.FullName),

            "email" => filter.Descending
                ? query.OrderByDescending(
                    profile => profile.User.NormalizedEmail)
                : query.OrderBy(
                    profile => profile.User.NormalizedEmail),

            _ => throw new ArgumentOutOfRangeException(
                nameof(filter.SortBy),
                "Unsupported client sorting field.")
        };

        var items = await orderedQuery
            .ThenBy(profile => profile.Id)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(profile => new ClientListItemResponse(
                profile.Id,
                profile.UserId,
                profile.User.FullName,
                profile.User.Email,
                profile.PhoneNumber,
                profile.User.IsActive,
                profile.DeliveryAddresses.Count,
                profile.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResponse<ClientListItemResponse>(
            filter.Page,
            filter.PageSize,
            totalCount,
            items);
    }

    public async Task<ClientDetailsResponse?> GetClientDetailsAsync(
        Guid customerProfileId,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.CustomerProfiles
            .AsNoTracking()
            .Where(profile =>
                profile.Id == customerProfileId)
            .Select(profile => new
            {
                profile.Id,
                profile.UserId,
                profile.User.FullName,
                profile.User.Email,
                profile.PhoneNumber,

                IsAccountActive = profile.User.IsActive,

                profile.CreatedAtUtc,
                profile.UpdatedAtUtc,

                Version = EF.Property<byte[]>(
                    profile,
                    "RowVersion"),

                Addresses = profile.DeliveryAddresses
                    .OrderBy(address => address.Label)
                    .ThenBy(address => address.Id)
                    .Select(address => new
                    {
                        address.Id,
                        address.Label,
                        address.RecipientName,
                        address.RecipientPhone,
                        address.City,
                        address.District,
                        address.StreetAddress,
                        address.BuildingDetails,
                        address.DeliveryInstructions,

                        Version = EF.Property<byte[]>(
                            address,
                            "RowVersion")
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (profile is null)
        {
            return null;
        }

        var addresses = profile.Addresses
            .Select(address => new DeliveryAddressResponse(
                address.Id,
                address.Label,
                address.RecipientName,
                address.RecipientPhone,
                address.City,
                address.District,
                address.StreetAddress,
                address.BuildingDetails,
                address.DeliveryInstructions,
                Convert.ToBase64String(address.Version)))
            .ToArray();

        return new ClientDetailsResponse(
            profile.Id,
            profile.UserId,
            profile.FullName,
            profile.Email,
            profile.PhoneNumber,
            profile.IsAccountActive,
            profile.CreatedAtUtc,
            profile.UpdatedAtUtc,
            Convert.ToBase64String(profile.Version),
            addresses);
    }

    public async Task<CustomerProfileResponse?> GetForUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.CustomerProfiles
            .AsNoTracking()
            .Where(profile => profile.UserId == userId)
            .Select(profile => new
            {
                profile.Id,
                profile.UserId,
                profile.User.FullName,
                profile.User.Email,
                profile.PhoneNumber,

                AddressCount = profile.DeliveryAddresses.Count,

                Version = EF.Property<byte[]>(
                    profile,
                    "RowVersion")
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (profile is null)
        {
            return null;
        }

        return new CustomerProfileResponse(
            profile.Id,
            profile.UserId,
            profile.FullName,
            profile.Email,
            profile.PhoneNumber,
            profile.AddressCount,
            Convert.ToBase64String(profile.Version));
    }

    public async Task<DeliveryAddressBookResponse?> GetAddressBookAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.CustomerProfiles
            .AsNoTracking()
            .Where(profile => profile.UserId == userId)
            .Select(profile => new
            {
                profile.Id,

                Version = EF.Property<byte[]>(
                    profile,
                    "RowVersion"),

                Addresses = profile.DeliveryAddresses
                    .OrderBy(address => address.CreatedAtUtc)
                    .ThenBy(address => address.Id)
                    .Select(address => new
                    {
                        address.Id,
                        address.Label,
                        address.RecipientName,
                        address.RecipientPhone,
                        address.City,
                        address.District,
                        address.StreetAddress,
                        address.BuildingDetails,
                        address.DeliveryInstructions,

                        Version = EF.Property<byte[]>(
                            address,
                            "RowVersion")
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (profile is null)
        {
            return null;
        }

        var addresses = profile.Addresses
            .Select(address => new DeliveryAddressResponse(
                address.Id,
                address.Label,
                address.RecipientName,
                address.RecipientPhone,
                address.City,
                address.District,
                address.StreetAddress,
                address.BuildingDetails,
                address.DeliveryInstructions,
                Convert.ToBase64String(address.Version)))
            .ToArray();

        return new DeliveryAddressBookResponse(
            profile.Id,
            Convert.ToBase64String(profile.Version),
            addresses);
    }

    public Task<bool> ExistsForUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return dbContext.CustomerProfiles
            .AsNoTracking()
            .AnyAsync(
                profile => profile.UserId == userId,
                cancellationToken);
    }

    public Task<CustomerProfile?> GetTrackedForUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return dbContext.CustomerProfiles
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM [dbo].[CustomerProfiles] WITH (UPDLOCK, HOLDLOCK)
                WHERE [UserId] = {userId}
                """)
            .AsTracking()
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<CustomerProfile?>
        GetTrackedWithAddressesForUserAsync(
            Guid userId,
            CancellationToken cancellationToken)
    {
        return dbContext.CustomerProfiles
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM [dbo].[CustomerProfiles] WITH (UPDLOCK, HOLDLOCK)
                WHERE [UserId] = {userId}
                """)
            .AsTracking()
            .Include(profile => profile.DeliveryAddresses)
            .AsSplitQuery()
            .SingleOrDefaultAsync(cancellationToken);
    }

    public byte[] GetVersion(CustomerProfile profile)
    {
        var version = dbContext.CustomerProfiles
            .Entry(profile)
            .Property<byte[]>("RowVersion")
            .CurrentValue;

        if (version is null || version.Length != 8)
        {
            throw new InvalidOperationException(
                "A persisted customer profile rowversion is required.");
        }

        return version;
    }

    public void SetExpectedVersion(
        CustomerProfile profile,
        byte[] version)
    {
        dbContext.CustomerProfiles
            .Entry(profile)
            .Property<byte[]>("RowVersion")
            .OriginalValue = version;
    }

    public void Add(CustomerProfile profile)
    {
        dbContext.CustomerProfiles.Add(profile);
    }
}