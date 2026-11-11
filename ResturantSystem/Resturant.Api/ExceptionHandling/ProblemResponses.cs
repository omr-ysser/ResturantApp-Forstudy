using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Resturant.Api.ExceptionHandling;

public static class ProblemResponses
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    public static async Task WriteAsync(
        HttpContext context,
        int statusCode,
        string title,
        string? detail = null,
        IDictionary<string, string[]>? errors = null)
    {
        if (context.Response.HasStarted ||
            context.RequestAborted.IsCancellationRequested)
        {
            return;
        }

        var problem = new ProblemDetails
        {
            Type = "about:blank",
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = context.Request.Path.Value
        };

        problem.Extensions["traceId"] =
            context.TraceIdentifier;

        if (errors is not null)
        {
            problem.Extensions["errors"] = errors;
        }

        context.Response.StatusCode = statusCode;

        var problemDetailsService = context.RequestServices
            .GetRequiredService<IProblemDetailsService>();

        var written = await problemDetailsService.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = problem
            });

        if (written)
        {
            return;
        }

        context.Response.ContentType =
            "application/problem+json";

        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            problem,
            JsonOptions,
            context.RequestAborted);
    }
}