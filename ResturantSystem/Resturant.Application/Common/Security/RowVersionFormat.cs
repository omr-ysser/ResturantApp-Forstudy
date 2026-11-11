using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Common.Security;

public static class RowVersionFormat
{
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            value.Length > 24)
        {
            return false;
        }

        try
        {
            return Convert.FromBase64String(value).Length == 8;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
