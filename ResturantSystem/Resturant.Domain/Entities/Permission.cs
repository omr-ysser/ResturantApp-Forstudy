using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Resturant.Domain.Common;
using Resturant.Domain.Exceptions;

namespace Resturant.Domain.Entities;

public sealed class Permission : BaseEntity
{
    private readonly List<RolePermission> _rolePermissions = [];

    private Permission()
    {
    }

    public Permission(string key, string displayName)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new DomainException("Permission key is required.");

        Key = key.Trim();
        NormalizedKey = Key.ToUpperInvariant();

        UpdateDisplayName(displayName);
    }

    // مثال: Orders.Create
    public string Key { get; private set; } = string.Empty;

    public string NormalizedKey { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public IReadOnlyCollection<RolePermission> RolePermissions =>
        _rolePermissions.AsReadOnly();

    public void UpdateDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Permission display name is required.");

        DisplayName = displayName.Trim();
        MarkUpdated();
    }
}
