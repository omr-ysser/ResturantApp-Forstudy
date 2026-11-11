using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Features.Dishes;

public static class DishInputRules
{
    public const decimal MaximumPrice =
        9_999_999_999_999_999.99m;

    public static bool IsValidPrice(decimal price)
    {
        return price > 0 &&
               price <= MaximumPrice &&
               decimal.Round(price, 2) == price;
    }

    public static bool IsValidVersion(string? version)
    {
        if (string.IsNullOrWhiteSpace(version) ||
            version.Length > 24)
        {
            return false;
        }

        try
        {
            return Convert.FromBase64String(version).Length == 8;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
