using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Restaurant.Application.Common.Models;
using Resturant.Domain.Entities;

namespace Restaurant.Application.Abstractions.Dao;

public interface IPermissionManagementDao
{
    Task<PermissionDetailsResponse?> GetDetailsAsync(
        Guid permissionId,
        CancellationToken cancellationToken);

    Task<Permission?> GetTrackedAsync(
        Guid permissionId,
        CancellationToken cancellationToken);

    Task<byte[]> GetVersionAsync(
        Guid permissionId,
        CancellationToken cancellationToken);

    Task<bool> ExistsAsync(
        string normalizedKey,
        CancellationToken cancellationToken);

    Task<bool> IsAssignedAsync(
        Guid permissionId,
        CancellationToken cancellationToken);

    void Add(Permission permission);

    void Remove(Permission permission);
}
