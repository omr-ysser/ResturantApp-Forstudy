using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Common.Models;

public sealed record PermissionDetailsResponse(
    Guid Id,
    string Key,
    string DisplayName,
    string Version,
    bool IsSystemPermission);

public sealed record CreatePermissionRequest(
    string Key,
    string DisplayName);

public sealed record UpdatePermissionRequest(
    string DisplayName,
    string Version);
