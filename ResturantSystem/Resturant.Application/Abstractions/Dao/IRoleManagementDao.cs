using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Resturant.Domain.Entities;

namespace Restaurant.Application.Abstractions.Dao;

public interface IRoleManagementDao
{
    Task<bool> NameExistsAsync(
        string normalizedName,
        Guid? excludingRoleId,
        CancellationToken cancellationToken);

    Task<Role?> GetTrackedAsync(
        Guid roleId,
        CancellationToken cancellationToken);

    Task<byte[]> GetVersionAsync(
        Guid roleId,
        CancellationToken cancellationToken);

    Task<bool> HasUsersAsync(
        Guid roleId,
        CancellationToken cancellationToken);

    void Add(Role role);

    void Remove(Role role);
}
