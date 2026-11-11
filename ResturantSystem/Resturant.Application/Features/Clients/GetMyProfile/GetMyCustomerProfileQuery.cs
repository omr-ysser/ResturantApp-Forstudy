using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Restaurant.Application.Abstractions.Authentication;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Common.Exceptions;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Features.Clients;

namespace Restaurant.Application.Features.Clients.GetMyProfile;

public sealed record GetMyCustomerProfileQuery
    : IRequest<CustomerProfileResponse>;

public sealed class GetMyCustomerProfileQueryHandler(
    ICustomerProfileDao customerProfileDao,
    ICurrentUser currentUser)
    : IRequestHandler<
        GetMyCustomerProfileQuery,
        CustomerProfileResponse>
{
    public async Task<CustomerProfileResponse> Handle(
        GetMyCustomerProfileQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.Access.Permissions.Contains(
                ClientPermissions.ViewOwn,
                StringComparer.Ordinal))
        {
            throw new ForbiddenOperationException();
        }

        return await customerProfileDao.GetForUserAsync(
                   currentUser.UserId,
                   cancellationToken)
               ?? throw new ResourceNotFoundException(
                   "Customer profile");
    }
}
