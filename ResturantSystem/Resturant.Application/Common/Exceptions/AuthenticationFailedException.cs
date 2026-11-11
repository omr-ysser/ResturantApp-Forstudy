using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Common.Exceptions;

public sealed class AuthenticationFailedException : Exception
{
    public AuthenticationFailedException()
        : base("Authentication failed.")
    {
    }
}