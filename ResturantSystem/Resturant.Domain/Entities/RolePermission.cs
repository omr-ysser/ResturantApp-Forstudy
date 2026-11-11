using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Resturant.Domain.Exceptions;

namespace Resturant.Domain.Entities;

public sealed class RolePermission
{
    private RolePermission()
    {
    }

    internal RolePermission(Guid roleId, Guid permissionId)
    {
        if (roleId == Guid.Empty || permissionId == Guid.Empty)
            throw new DomainException("Role and permission are required.");

        RoleId = roleId;
        PermissionId = permissionId;
    }

    public Guid RoleId { get; private set; }

    public Role Role { get; private set; } = null!;

    public Guid PermissionId { get; private set; }

    public Permission Permission { get; private set; } = null!;
}
