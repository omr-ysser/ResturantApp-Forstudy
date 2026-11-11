using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Common.Models;

public sealed record CreateUserRequest(
    string FullName,
    string Email,
    string Password,
    Guid[] RoleIds)
{
    public override string ToString()
    {
        return nameof(CreateUserRequest);
    }
}

public sealed record UpdateUserProfileRequest(
    string FullName,
    string Email,
    string Version);
