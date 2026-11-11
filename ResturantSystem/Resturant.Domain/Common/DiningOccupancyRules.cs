using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resturant.Domain.Common;

public static class DiningOccupancyRules
{
    public const int DefaultWalkInDurationMinutes = 120;

    public static readonly TimeSpan DefaultWalkInDuration =
        TimeSpan.FromMinutes(DefaultWalkInDurationMinutes);
}
