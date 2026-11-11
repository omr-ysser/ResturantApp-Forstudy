using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resturant.Domain.Enums;

public enum ReservationStatus
{
    Confirmed = 1,
    Seated = 2,
    Completed = 3,
    Cancelled = 4,
    NoShow = 5
}
