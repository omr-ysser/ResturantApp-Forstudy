using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Restaurant.Application.Common.Models;

namespace Restaurant.Application.Abstractions.Services;

public interface IUserAccessService
{
    Task<UserAccessResponse> SetRolesAsync(
        Guid userId,
        string version,
        Guid[] roleIds,
        CancellationToken cancellationToken);

    Task<UserAccessResponse> SetStatusAsync(
        Guid userId,
        string version,
        bool isActive,
        CancellationToken cancellationToken);
}
