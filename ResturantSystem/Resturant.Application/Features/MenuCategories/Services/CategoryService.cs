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

namespace Restaurant.Application.Features.MenuCategories.Services;

public sealed class CategoryService(
    ICategoryDao categoryDao,
    IAppDbContext dbContext,
    ICurrentUser currentUser,
    ILogger<CategoryService> logger)
    : ICategoryService
{
    public async Task<CategoryResponse> CreateAsync(
        string name,
        string? description,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await dbContext.BeginTransactionAsync(
                cancellationToken);

        var exists = await categoryDao.NameExistsAsync(
            name,
            excludingCategoryId: null,
            cancellationToken: cancellationToken);

        if (exists)
        {
            throw new BusinessConflictException(
                "A category with this name already exists.");
        }

        var category = new Category(
            name,
            description);

        categoryDao.Add(category);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        var response = await GetRequiredAsync(
            category.Id,
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        logger.LogInformation(
            "Category {CategoryId} created by user {UserId}",
            category.Id,
            currentUser.UserId);

        return response;
    }

    public async Task<CategoryResponse> UpdateAsync(
        Guid categoryId,
        string name,
        string? description,
        string version,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await dbContext.BeginTransactionAsync(
                cancellationToken);

        var category = await LockRequiredAsync(
            categoryId,
            cancellationToken);

        await EnsureVersionAsync(
            categoryId,
            version,
            cancellationToken);

        var exists = await categoryDao.NameExistsAsync(
            name,
            excludingCategoryId: categoryId,
            cancellationToken: cancellationToken);

        if (exists)
        {
            throw new BusinessConflictException(
                "A category with this name already exists.");
        }

        category.UpdateDetails(
            name,
            description);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        var response = await GetRequiredAsync(
            categoryId,
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        logger.LogInformation(
            "Category {CategoryId} updated by user {UserId}",
            categoryId,
            currentUser.UserId);

        return response;
    }

    public async Task<CategoryResponse> SetStatusAsync(
        Guid categoryId,
        bool isActive,
        string version,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await dbContext.BeginTransactionAsync(
                cancellationToken);

        var category = await LockRequiredAsync(
            categoryId,
            cancellationToken);

        await EnsureVersionAsync(
            categoryId,
            version,
            cancellationToken);

        if (category.IsActive != isActive)
        {
            category.SetActive(isActive);
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);

        var response = await GetRequiredAsync(
            categoryId,
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        logger.LogInformation(
            "Category {CategoryId} active status set to {IsActive} " +
            "by user {UserId}",
            categoryId,
            isActive,
            currentUser.UserId);

        return response;
    }

    public async Task DeleteAsync(
        Guid categoryId,
        string version,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await dbContext.BeginTransactionAsync(
                cancellationToken);

        var category = await LockRequiredAsync(
            categoryId,
            cancellationToken);

        await EnsureVersionAsync(
            categoryId,
            version,
            cancellationToken);

        var hasItems = await categoryDao.HasMenuItemsAsync(
            categoryId,
            cancellationToken);

        if (hasItems)
        {
            throw new BusinessConflictException(
                "A category containing menu items cannot be deleted. " +
                "Deactivate it instead.");
        }

        categoryDao.Remove(category);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        logger.LogInformation(
            "Category {CategoryId} deleted by user {UserId}",
            categoryId,
            currentUser.UserId);
    }

    private async Task<Category> LockRequiredAsync(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        return await categoryDao.LockForUpdateAsync(
                   categoryId,
                   cancellationToken)
               ?? throw new ResourceNotFoundException(
                   "Category");
    }

    private async Task<CategoryResponse> GetRequiredAsync(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        return await categoryDao.GetAsync(
                   categoryId,
                   cancellationToken)
               ?? throw new ResourceNotFoundException(
                   "Category");
    }

    private async Task EnsureVersionAsync(
        Guid categoryId,
        string expectedVersion,
        CancellationToken cancellationToken)
    {
        var currentVersion =
            await categoryDao.GetVersionAsync(
                categoryId,
                cancellationToken);

        var suppliedVersion =
            Convert.FromBase64String(expectedVersion);

        if (!currentVersion.SequenceEqual(suppliedVersion))
        {
            throw new BusinessConflictException(
                "The category changed. Reload it before updating.");
        }
    }
}
