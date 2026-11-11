using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Restaurant.Application.Common.Models;
using Resturant.Domain.Entities;

namespace Restaurant.Application.Abstractions.Dao;

public interface IRolePermissionDao
{
    Task<PagedResponse<RoleSummaryResponse>> ListRolesAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken);

    Task<RolePermissionsResponse?> GetRoleDetailsAsync(
        Guid roleId,
        CancellationToken cancellationToken);

    Task<Role?> LockAdminRoleAsync(
        CancellationToken cancellationToken);

    Task<bool> IsActiveAdminAsync(
        Guid userId,
        Guid adminRoleId,
        CancellationToken cancellationToken);

    Task<Role?> GetTrackedRoleAsync(
        Guid roleId,
        CancellationToken cancellationToken);

    Task<byte[]> GetRoleVersionAsync(
        Guid roleId,
        CancellationToken cancellationToken);

    Task<List<Permission>> GetPermissionsAsync(
        Guid[] permissionIds,
        CancellationToken cancellationToken);
}
