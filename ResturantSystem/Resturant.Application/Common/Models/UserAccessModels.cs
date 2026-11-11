using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Common.Models;

public sealed record UserSummaryResponse(
    Guid Id,
    string FullName,
    string Email,
    bool IsActive);

public sealed record UserAccessResponse(
    Guid Id,
    string FullName,
    string Email,
    bool IsActive,
    string Version,
    IReadOnlyList<RoleSummaryResponse> Roles);

public sealed record SetUserRolesRequest(
    string Version,
    Guid[] RoleIds);

public sealed record SetUserStatusRequest(
    string Version,
    bool IsActive);
