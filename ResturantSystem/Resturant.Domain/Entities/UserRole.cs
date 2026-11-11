using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Resturant.Domain.Exceptions;

namespace Resturant.Domain.Entities;

public sealed class UserRole
{
    private UserRole()
    {
    }

    internal UserRole(Guid userId, Guid roleId)
    {
        if (userId == Guid.Empty || roleId == Guid.Empty)
            throw new DomainException("User and role are required.");

        UserId = userId;
        RoleId = roleId;
    }

    public Guid UserId { get; private set; }

    public User User { get; private set; } = null!;

    public Guid RoleId { get; private set; }

    public Role Role { get; private set; } = null!;
}
