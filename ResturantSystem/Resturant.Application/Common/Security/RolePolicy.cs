using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Common.Security;

public static class RolePolicy
{
    public static bool IsSystemRole(
        string normalizedName)
    {
        return normalizedName.ToUpperInvariant() switch
        {
            "ADMIN" => true,
            "SUPERVISOR" => true,
            "VENDOR" => true,
            "CUSTOMER" => true,
            _ => false
        };
    }
}
