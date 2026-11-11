using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using MediatR;
using Restaurant.Application.Abstractions.Authentication;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Common.Exceptions;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Features.Clients;

namespace Restaurant.Application.Features.Clients.List;

public sealed record ListClientsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    bool? IsAccountActive = null,
    string SortBy = "createdAt",
    bool Descending = true)
    : IRequest<PagedResponse<ClientListItemResponse>>
{
    public override string ToString() =>
        nameof(ListClientsQuery);
}

public sealed class ListClientsQueryValidator
    : AbstractValidator<ListClientsQuery>
{
    public ListClientsQueryValidator()
    {
        RuleFor(query => query.Page)
            .InclusiveBetween(1, 100_000);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(query => query.Search)
            .MaximumLength(256);

        RuleFor(query => query.SortBy)
            .Must(sortBy =>
                string.Equals(
                    sortBy,
                    "createdAt",
                    StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(
                    sortBy,
                    "name",
                    StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(
                    sortBy,
                    "email",
                    StringComparison.OrdinalIgnoreCase))
            .WithMessage(
                "SortBy must be createdAt, name, or email.");
    }
}

public sealed class ListClientsQueryHandler(
    ICustomerProfileDao customerProfileDao,
    ICurrentUser currentUser)
    : IRequestHandler<
        ListClientsQuery,
        PagedResponse<ClientListItemResponse>>
{
    public Task<PagedResponse<ClientListItemResponse>> Handle(
        ListClientsQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.Access.Permissions.Contains(
                ClientPermissions.View,
                StringComparer.Ordinal))
        {
            throw new ForbiddenOperationException();
        }

        var filter = new ClientListFilter(
            request.Page,
            request.PageSize,
            request.Search,
            request.IsAccountActive,
            request.SortBy,
            request.Descending);

        return customerProfileDao.ListClientsAsync(
            filter,
            cancellationToken);
    }
}
