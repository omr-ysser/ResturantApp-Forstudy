using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Common.Models;

public sealed record RoleSummaryResponse(
    Guid Id,
    string Name);

public sealed record RolePermissionsResponse(
    Guid Id,
    string Name,
    string Version,
    IReadOnlyList<PermissionResponse> Permissions);

public sealed record SetRolePermissionsRequest(
    string Version,
    Guid[] PermissionIds);
