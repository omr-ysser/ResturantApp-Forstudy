using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Restaurant.Application.Common.Models;

namespace Restaurant.Application.Abstractions.Dao;

public interface IPasswordCredentialDao
{
    Task<LoginUser?> FindAsync(
        Guid userId,
        CancellationToken cancellationToken);
}
