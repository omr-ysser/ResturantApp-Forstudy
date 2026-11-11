using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Restaurant.Application.Common.Models;

namespace Restaurant.Application.Abstractions.Services;

public interface IRoleManagementService
{
    Task<RolePermissionsResponse> CreateAsync(
        string name,
        CancellationToken cancellationToken);

    Task<RolePermissionsResponse> UpdateAsync(
        Guid roleId,
        string name,
        string version,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        Guid roleId,
        string version,
        CancellationToken cancellationToken);
}
