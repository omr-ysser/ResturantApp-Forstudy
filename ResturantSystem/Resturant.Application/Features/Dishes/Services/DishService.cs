using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Restaurant.Application.Abstractions.Authentication;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Abstractions.Persistence;
using Restaurant.Application.Abstractions.Services;
using Restaurant.Application.Common.Exceptions;
using Restaurant.Application.Common.Models;
using Resturant.Domain.Entities;

namespace Restaurant.Application.Features.Dishes.Services;

public sealed class DishService(
    IDishManagementDao dishManagementDao,
    IDishReadDao dishReadDao,
    ICategoryDao categoryDao,
    IAppDbContext dbContext,
    ICurrentUser currentUser,
    ILogger<DishService> logger)
    : IDishService
{
    public async Task<DishResponse> CreateAsync(
        Guid categoryId,
        string name,
        decimal price,
        string? description,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await dbContext.BeginTransactionAsync(
                cancellationToken);

        var category = await categoryDao.LockForUpdateAsync(
            categoryId,
            cancellationToken);

        if (category is null)
        {
            throw new ResourceNotFoundException("Category");
        }

        EnsureCategoryIsActive(category);

        await EnsureNameIsUniqueAsync(
            categoryId,
            name,
            excludingId: null,
            cancellationToken);

        var dish = new MenuItem(
            categoryId,
            name,
            price,
            description);

        dishManagementDao.Add(dish);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        var response = await GetRequiredResponseAsync(
            dish.Id,
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        logger.LogInformation(
            "Dish {DishId} created in category {CategoryId} by user {UserId}",
            dish.Id,
            categoryId,
            currentUser.UserId);

        return response;
    }

    public async Task<DishResponse> UpdateAsync(
        Guid id,
        string name,
        decimal price,
        string? description,
        string version,
        CancellationToken cancellationToken)
    {
        var expectedVersion = ParseVersion(version);

        await using var transaction =
            await dbContext.BeginTransactionAsync(
                cancellationToken);

        var (dish, category) = await LoadForMutationAsync(
            id,
            expectedVersion,
            cancellationToken);

        EnsureCategoryIsActive(category);

        await EnsureNameIsUniqueAsync(
            dish.CategoryId,
            name,
            dish.Id,
            cancellationToken);

        dishManagementDao.SetExpectedVersion(
            dish,
            expectedVersion);

        dish.UpdateDetails(
            name,
            description);

        dish.ChangePrice(price);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        var response = await GetRequiredResponseAsync(
            dish.Id,
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        logger.LogInformation(
            "Dish {DishId} updated by user {UserId}",
            dish.Id,
            currentUser.UserId);

        return response;
    }

    public async Task<DishResponse> SetAvailabilityAsync(
        Guid id,
        bool isAvailable,
        string version,
        CancellationToken cancellationToken)
    {
        var expectedVersion = ParseVersion(version);

        await using var transaction =
            await dbContext.BeginTransactionAsync(
                cancellationToken);

        var (dish, category) = await LoadForMutationAsync(
            id,
            expectedVersion,
            cancellationToken);

        if (isAvailable)
        {
            EnsureCategoryIsActive(category);
        }

        dishManagementDao.SetExpectedVersion(
            dish,
            expectedVersion);

        // Always call the domain method so the mutation participates
        // in the rowversion check even when the value is unchanged.
        dish.SetAvailability(isAvailable);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        var response = await GetRequiredResponseAsync(
            dish.Id,
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        logger.LogInformation(
            "Dish {DishId} availability set to {IsAvailable} by user {UserId}",
            dish.Id,
            isAvailable,
            currentUser.UserId);

        return response;
    }

    private async Task<(MenuItem Dish, Category Category)>
        LoadForMutationAsync(
            Guid id,
            byte[] expectedVersion,
            CancellationToken cancellationToken)
    {
        var snapshot = await dishReadDao.GetAsync(
            id,
            cancellationToken);

        if (snapshot is null)
        {
            throw new ResourceNotFoundException("Dish");
        }

        var currentVersion = Convert.FromBase64String(
            snapshot.Version);

        if (!currentVersion.AsSpan().SequenceEqual(
                expectedVersion))
        {
            throw new BusinessConflictException(
                "The dish has changed. Reload it and try again.");
        }

        // Use the same category lock as category management.
        // This keeps category activation/deletion coordinated
        // with dish mutations until the transaction finishes.
        var category = await categoryDao.LockForUpdateAsync(
            snapshot.CategoryId,
            cancellationToken);

        if (category is null)
        {
            throw new ResourceNotFoundException("Category");
        }

        var dish = await dishManagementDao.GetTrackedAsync(
            id,
            cancellationToken);

        if (dish is null)
        {
            throw new ResourceNotFoundException("Dish");
        }

        if (dish.CategoryId != snapshot.CategoryId)
        {
            throw new BusinessConflictException(
                "The dish category has changed. Reload it and try again.");
        }

        return (dish, category);
    }

    private async Task EnsureNameIsUniqueAsync(
        Guid categoryId,
        string name,
        Guid? excludingId,
        CancellationToken cancellationToken)
    {
        var exists = await dishManagementDao.NameExistsAsync(
            categoryId,
            name,
            excludingId,
            cancellationToken);

        if (exists)
        {
            throw new BusinessConflictException(
                "A dish with this name already exists in the category.");
        }
    }

    private async Task<DishResponse> GetRequiredResponseAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await dishReadDao.GetAsync(
            id,
            cancellationToken);

        return response
            ?? throw new ResourceNotFoundException("Dish");
    }

    private static void EnsureCategoryIsActive(
        Category category)
    {
        if (!category.IsActive)
        {
            throw new BusinessConflictException(
                "The category is inactive. Activate it before creating, " +
                "editing, or enabling its dishes.");
        }
    }

    private static byte[] ParseVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version) ||
            version.Length > 24)
        {
            throw new BusinessConflictException(
                "The dish version is invalid. Reload it and try again.");
        }

        try
        {
            var bytes = Convert.FromBase64String(version);

            if (bytes.Length == 8)
            {
                return bytes;
            }
        }
        catch (FormatException)
        {
            // Return a controlled application conflict below.
        }

        throw new BusinessConflictException(
            "The dish version is invalid. Reload it and try again.");
    }
}
