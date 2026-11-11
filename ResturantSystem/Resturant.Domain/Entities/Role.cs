using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Resturant.Domain.Common;
using Resturant.Domain.Exceptions;

namespace Resturant.Domain.Entities;

public sealed class Role : BaseEntity
{
    private readonly List<RolePermission> _rolePermissions = [];
    private readonly List<UserRole> _userRoles = [];

    private Role()
    {
    }

    public Role(string name)
    {
        Rename(name);
    }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public IReadOnlyCollection<RolePermission> RolePermissions =>
        _rolePermissions.AsReadOnly();

    public IReadOnlyCollection<UserRole> UserRoles =>
        _userRoles.AsReadOnly();

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Role name is required.");

        Name = name.Trim();
        NormalizedName = Name.ToUpperInvariant();

        MarkUpdated();
    }

    public void GrantPermission(Guid permissionId)
    {
        if (permissionId == Guid.Empty)
            throw new DomainException("Permission is required.");

        if (_rolePermissions.Any(item =>
                item.PermissionId == permissionId))
        {
            return;
        }

        _rolePermissions.Add(new RolePermission(Id, permissionId));
        MarkUpdated();
    }

    public void RevokePermission(Guid permissionId)
    {
        var rolePermission = _rolePermissions.SingleOrDefault(
            item => item.PermissionId == permissionId);

        if (rolePermission is null)
            return;

        _rolePermissions.Remove(rolePermission);
        MarkUpdated();
    }
}
