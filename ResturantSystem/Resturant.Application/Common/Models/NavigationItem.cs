using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Common.Models;

public sealed record NavigationItem(
    string Key,
    string Label,
    string Route,
    string Icon,
    IReadOnlyList<string> RequiredPermissions);
