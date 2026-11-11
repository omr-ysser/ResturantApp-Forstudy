using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Common.Exceptions;

public sealed class ForbiddenOperationException : Exception
{
    public ForbiddenOperationException()
        : base("You are not allowed to perform this operation.")
    {
    }
}

public sealed class ResourceNotFoundException : Exception
{
    public ResourceNotFoundException(string resource)
        : base($"{resource} was not found.")
    {
    }
}

public sealed class BusinessConflictException : Exception
{
    public BusinessConflictException(string message)
        : base(message)
    {
    }
}
