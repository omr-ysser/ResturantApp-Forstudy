using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Restaurant.Application.Common.Exceptions;
using Resturant.Domain.Exceptions;

namespace Resturant.Api.ExceptionHandling;

public sealed class ApiExceptionHandler(
    ILogger<ApiExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // The client disconnected or canceled the request.
        if (exception is OperationCanceledException &&
            httpContext.RequestAborted.IsCancellationRequested)
        {
            return true;
        }

        // FluentValidation request validation errors.
        if (exception is ValidationException validation)
        {
            var errors = validation.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .Select(error => error.ErrorMessage)
                        .Distinct()
                        .ToArray());

            await ProblemResponses.WriteAsync(
                httpContext,
                StatusCodes.Status400BadRequest,
                "Validation failed",
                "One or more request fields are invalid.",
                errors);

            return true;
        }

        // Invalid credentials or an invalid authentication session.
        if (exception is AuthenticationFailedException)
        {
            httpContext.Response.Headers["WWW-Authenticate"] = "Bearer";

            await ProblemResponses.WriteAsync(
                httpContext,
                StatusCodes.Status401Unauthorized,
                "Authentication failed",
                "The supplied credentials are invalid.");

            return true;
        }

        // The authenticated user cannot perform this operation.
        if (exception is ForbiddenOperationException)
        {
            await ProblemResponses.WriteAsync(
                httpContext,
                StatusCodes.Status403Forbidden,
                "Forbidden",
                "You are not allowed to perform this operation.");

            return true;
        }

        // The requested resource does not exist.
        if (exception is ResourceNotFoundException notFound)
        {
            await ProblemResponses.WriteAsync(
                httpContext,
                StatusCodes.Status404NotFound,
                "Not found",
                notFound.Message);

            return true;
        }

        // An application business rule prevents the operation.
        if (exception is BusinessConflictException conflict)
        {
            await ProblemResponses.WriteAsync(
                httpContext,
                StatusCodes.Status409Conflict,
                "Business rule conflict",
                conflict.Message);

            return true;
        }

        // Keep this before DbUpdateException:
        // DbUpdateConcurrencyException derives from DbUpdateException.
        if (exception is DbUpdateConcurrencyException)
        {
            await ProblemResponses.WriteAsync(
                httpContext,
                StatusCodes.Status409Conflict,
                "Concurrency conflict",
                "The resource changed. Reload it and try again.");

            return true;
        }

        // SQL Server unique index or unique constraint violation.
        if (exception is DbUpdateException
            {
                InnerException: SqlException
                {
                    Number: 2601 or 2627
                }
            })
        {
            await ProblemResponses.WriteAsync(
                httpContext,
                StatusCodes.Status409Conflict,
                "Duplicate resource",
                "A resource with the same unique value already exists.");

            return true;
        }

        // SQL Server foreign key or CHECK constraint violation.
        // This also handles a relationship conflict occurring after
        // an application-level check, such as a concurrent insert.
        if (exception is DbUpdateException
            {
                InnerException: SqlException
                {
                    Number: 547
                }
            })
        {
            await ProblemResponses.WriteAsync(
                httpContext,
                StatusCodes.Status409Conflict,
                "Data constraint conflict",
                "The operation conflicts with related data or a database constraint.");

            return true;
        }

        // A domain invariant prevents the operation.
        if (exception is DomainException)
        {
            await ProblemResponses.WriteAsync(
                httpContext,
                StatusCodes.Status409Conflict,
                "Business rule conflict",
                "The operation conflicts with a business rule.");

            return true;
        }

        // Log unexpected failures without exposing internal details
        // in the response.
        logger.LogError(
            exception,
            "Unhandled request failure. TraceId: {TraceId}",
            httpContext.TraceIdentifier);

        await ProblemResponses.WriteAsync(
            httpContext,
            StatusCodes.Status500InternalServerError,
            "Internal server error",
            "An unexpected error occurred.");

        return true;
    }
}