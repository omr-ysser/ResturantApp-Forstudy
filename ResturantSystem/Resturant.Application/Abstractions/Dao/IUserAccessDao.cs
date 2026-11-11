using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Restaurant.Application.Common.Models;
using Resturant.Domain.Entities;

namespace Restaurant.Application.Abstractions.Dao;

public interface IUserAccessDao
{
    Task<PagedResponse<UserSummaryResponse>> ListAsync(
        int page,
        int pageSize,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken);

    Task<UserAccessResponse?> GetDetailsAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<User?> GetTrackedWithRolesAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<byte[]> GetVersionAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<List<Role>> GetRolesAsync(
        Guid[] roleIds,
        CancellationToken cancellationToken);

    Task<int> CountActiveAdminsAsync(
        Guid adminRoleId,
        CancellationToken cancellationToken);
}
