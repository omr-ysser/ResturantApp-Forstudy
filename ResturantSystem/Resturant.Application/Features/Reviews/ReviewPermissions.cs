using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Features.Reviews;

public static class ReviewPermissions
{
    public const string View =
        "Reviews.View";

    public const string CreateOwn =
        "Reviews.CreateOwn";

    public const string EditOwn =
        "Reviews.EditOwn";

    public const string DeleteOwn =
        "Reviews.DeleteOwn";

    public const string Moderate =
        "Reviews.Moderate";
}
