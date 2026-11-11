using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Resturant.Domain.Entities;

namespace Restaurant.Application.Abstractions.Dao;

public interface IUserAccountDao
{
    Task<bool> EmailExistsAsync(
        string normalizedEmail,
        Guid? excludingUserId,
        CancellationToken cancellationToken);

    void Add(User user);
}
