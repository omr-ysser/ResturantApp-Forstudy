using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Restaurant.Application.Common.Models;

namespace Restaurant.Application.Abstractions.Services;

public interface IRolePermissionService
{
    Task<RolePermissionsResponse> SetPermissionsAsync(
        Guid roleId,
        string version,
        Guid[] permissionIds,
        CancellationToken cancellationToken);
}
