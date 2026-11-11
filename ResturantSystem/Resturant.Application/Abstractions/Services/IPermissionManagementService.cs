using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Restaurant.Application.Common.Models;

namespace Restaurant.Application.Abstractions.Services;

public interface IPermissionManagementService
{
    Task<PermissionDetailsResponse> CreateAsync(
        string key,
        string displayName,
        CancellationToken cancellationToken);

    Task<PermissionDetailsResponse> UpdateAsync(
        Guid permissionId,
        string displayName,
        string version,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        Guid permissionId,
        string version,
        CancellationToken cancellationToken);
}
