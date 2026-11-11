using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resturant.Domain.Common;

public static class ReviewRules
{
    public const int MinimumRating = 1;

    public const int MaximumRating = 5;

    public const int MaximumCommentLength = 2000;

    public const int MaximumModerationReasonLength = 500;
}
