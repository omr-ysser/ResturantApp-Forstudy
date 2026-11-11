using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Restaurant.Application.Common.Models;

namespace Restaurant.Application.Abstractions.Services;

public interface IUserAccountService
{
    Task<UserAccessResponse> CreateAsync(
        string fullName,
        string email,
        string password,
        Guid[] roleIds,
        CancellationToken cancellationToken);

    Task<UserAccessResponse> UpdateProfileAsync(
        Guid userId,
        string fullName,
        string email,
        string version,
        CancellationToken cancellationToken);
}
