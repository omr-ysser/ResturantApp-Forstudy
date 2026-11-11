using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text;

namespace Restaurant.Application.Common.Security;

public static class PasswordRules
{
    public const int MinimumLength = 15;
    public const int MaximumLength = 128;

    public static bool HasValidLength(string? password)
    {
        if (password is null)
        {
            return false;
        }

        var count = 0;

        foreach (var rune in password.EnumerateRunes())
        {
            count++;

            if (count > MaximumLength)
            {
                return false;
            }
        }

        return count >= MinimumLength;
    }
}