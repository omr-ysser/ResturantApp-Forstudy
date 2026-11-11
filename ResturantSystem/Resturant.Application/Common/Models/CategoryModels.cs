using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.Json.Serialization;

namespace Restaurant.Application.Common.Models;

public sealed record CategoryResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    int MenuItemsCount,
    string Version);

public sealed record CreateCategoryRequest(
    string Name,
    string? Description);

public sealed record UpdateCategoryRequest(
    string Name,
    string? Description,
    string Version);

public sealed record SetCategoryStatusRequest(
    string Version,
    [property: JsonRequired] bool IsActive);
