using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Resturant.Domain.Entities;

namespace Restaurant.Application.Abstractions.Dao;

public interface IDishManagementDao
{
    Task<MenuItem?> GetTrackedAsync(
        Guid id,
        CancellationToken cancellationToken);

    void SetExpectedVersion(
        MenuItem dish,
        byte[] expectedVersion);

    Task<bool> NameExistsAsync(
        Guid categoryId,
        string name,
        Guid? excludingId,
        CancellationToken cancellationToken);

    void Add(MenuItem dish);
}
