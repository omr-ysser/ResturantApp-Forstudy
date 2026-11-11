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

namespace Restaurant.Application.Features.Clients.Get;

public sealed record GetClientQuery(Guid Id)
    : IRequest<ClientDetailsResponse>;

public sealed class GetClientQueryValidator
    : AbstractValidator<GetClientQuery>
{
    public GetClientQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty();
    }
}

public sealed class GetClientQueryHandler(
    ICustomerProfileDao customerProfileDao,
    ICurrentUser currentUser)
    : IRequestHandler<
        GetClientQuery,
        ClientDetailsResponse>
{
    public async Task<ClientDetailsResponse> Handle(
        GetClientQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.Access.Permissions.Contains(
                ClientPermissions.View,
                StringComparer.Ordinal))
        {
            throw new ForbiddenOperationException();
        }

        return await customerProfileDao.GetClientDetailsAsync(
                   request.Id,
                   cancellationToken)
               ?? throw new ResourceNotFoundException(
                   "Customer profile");
    }
}
