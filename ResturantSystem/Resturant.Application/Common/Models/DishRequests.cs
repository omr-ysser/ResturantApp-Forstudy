using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.Json.Serialization;

namespace Restaurant.Application.Common.Models;

public sealed record CreateDishRequest(
    Guid CategoryId,
    string Name,
    decimal Price,
    string? Description);

public sealed record UpdateDishRequest(
    string Name,
    decimal Price,
    string? Description,
    string Version);

public sealed record SetDishAvailabilityRequest(
    string Version,
    [property: JsonRequired] bool IsAvailable);
