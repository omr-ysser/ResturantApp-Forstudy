using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Restaurant.Application.Common.Models;

namespace Restaurant.Application.Abstractions.Dao;

public interface IAuthenticationDao
{
    Task<LoginUser?> FindLoginUserAsync(
        string normalizedEmail,
        CancellationToken cancellationToken);

    Task<AccessProfile?> GetAccessAsync(
        Guid userId,
        CancellationToken cancellationToken);
}
