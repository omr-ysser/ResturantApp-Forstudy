using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Common.Models;

public sealed record UpdateReviewRequest(
    int Rating,
    string? Comment,
    string Version)
{
    public override string ToString() =>
        nameof(UpdateReviewRequest);
}
