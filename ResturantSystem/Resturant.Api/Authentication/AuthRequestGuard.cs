using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Resturant.Api.Authentication;

public sealed class AuthRequestGuard(
    IOptions<BrowserSecurityOptions> options)
    : IAuthorizationFilter
{
    public const string HeaderName =
        "X-Resturant-CSRF";

    public void OnAuthorization(
        AuthorizationFilterContext context)
    {
        var request = context.HttpContext.Request;

        if (!request.IsHttps)
        {
            Reject(
                context,
                "HTTPS is required.");

            return;
        }

        var header = request.Headers[HeaderName];

        if (header.Count != 1 || header[0] != "1")
        {
            Reject(
                context,
                "The authentication request header is required.");

            return;
        }

        var origin = request.Headers["Origin"];

        if (origin.Count == 0)
        {
            // Non-browser clients may omit Origin.
            // The custom header is still required.
            return;
        }

        if (origin.Count != 1 ||
            !options.Value.AllowedOrigins.Contains(
                origin[0],
                StringComparer.OrdinalIgnoreCase))
        {
            Reject(
                context,
                "The request origin is not allowed.");
        }
    }

    private static void Reject(
        AuthorizationFilterContext context,
        string detail)
    {
        var problem = new ProblemDetails
        {
            Type = "about:blank",
            Title = "Forbidden",
            Status = StatusCodes.Status403Forbidden,
            Detail = detail,
            Instance = context.HttpContext.Request.Path.Value
        };

        problem.Extensions["traceId"] =
            context.HttpContext.TraceIdentifier;

        var result = new ObjectResult(problem)
        {
            StatusCode = StatusCodes.Status403Forbidden
        };

        result.ContentTypes.Add(
            "application/problem+json");

        context.Result = result;
    }
}
