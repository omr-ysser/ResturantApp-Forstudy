using Restaurant.Application.Abstractions.Authentication;
using Restaurant.Application.Common.Exceptions;
using Restaurant.Application.Common.Models;

namespace Resturant.Api.Authentication;

public sealed class CurrentUser(
    IHttpContextAccessor httpContextAccessor)
    : ICurrentUser
{
    public Guid UserId => Access.Id;

    public Guid SessionId
    {
        get
        {
            var subject = httpContextAccessor.HttpContext?
                .User.FindFirst("sid")?.Value;

            if (!Guid.TryParse(subject, out var sessionId))
            {
                throw new AuthenticationFailedException();
            }

            return sessionId;
        }
    }

    public AccessProfile Access
    {
        get
        {
            var context =
                httpContextAccessor.HttpContext;

            if (context?.Items[typeof(AccessProfile)]
                is not AccessProfile access)
            {
                throw new AuthenticationFailedException();
            }

            return access;
        }
    }
}