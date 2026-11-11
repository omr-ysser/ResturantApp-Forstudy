using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Restaurant.Application.Common.Models;


namespace Restaurant.Application.Abstractions.Authentication;

public interface ICurrentUser
{
    Guid UserId { get; }

    Guid SessionId { get; }

    AccessProfile Access { get; }
}
