using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Common.Models;

public sealed record CreateRoleRequest(
    string Name);

public sealed record UpdateRoleRequest(
    string Name,
    string Version);
