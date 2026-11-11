using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.Json.Serialization;

namespace Restaurant.Application.Common.Models;

public sealed record ModerateReviewRequest(
    [property: JsonRequired] bool IsHidden,
    string Reason,
    string Version)
{
    public override string ToString() =>
        nameof(ModerateReviewRequest);
}
