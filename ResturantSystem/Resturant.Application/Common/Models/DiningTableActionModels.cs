using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.Json.Serialization;

namespace Restaurant.Application.Common.Models;

public sealed record UpdateDiningTableRequest(
    int Number,
    int Capacity,
    string Version);

public sealed record SetDiningTableServiceStateRequest(
    [property: JsonRequired] bool IsInService,
    string Version);
