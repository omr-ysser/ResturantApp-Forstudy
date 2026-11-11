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

namespace Restaurant.Application.Features.Clients.Addresses;

public sealed record GetMyDeliveryAddressesQuery
    : IRequest<DeliveryAddressBookResponse>;

public sealed class GetMyDeliveryAddressesQueryHandler(
    ICustomerProfileDao customerProfileDao,
    ICurrentUser currentUser)
    : IRequestHandler<
        GetMyDeliveryAddressesQuery,
        DeliveryAddressBookResponse>
{
    public async Task<DeliveryAddressBookResponse> Handle(
        GetMyDeliveryAddressesQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.Access.Permissions.Contains(
                ClientPermissions.ViewOwn,
                StringComparer.Ordinal))
        {
            throw new ForbiddenOperationException();
        }

        return await customerProfileDao.GetAddressBookAsync(
                   currentUser.UserId,
                   cancellationToken)
               ?? throw new ResourceNotFoundException(
                   "Customer profile");
    }
}

public sealed record GetMyDeliveryAddressQuery(Guid Id)
    : IRequest<DeliveryAddressChangeResponse>;

public sealed class GetMyDeliveryAddressQueryValidator
    : AbstractValidator<GetMyDeliveryAddressQuery>
{
    public GetMyDeliveryAddressQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty();
    }
}

public sealed class GetMyDeliveryAddressQueryHandler(
    ICustomerProfileDao customerProfileDao,
    ICurrentUser currentUser)
    : IRequestHandler<
        GetMyDeliveryAddressQuery,
        DeliveryAddressChangeResponse>
{
    public async Task<DeliveryAddressChangeResponse> Handle(
        GetMyDeliveryAddressQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.Access.Permissions.Contains(
                ClientPermissions.ViewOwn,
                StringComparer.Ordinal))
        {
            throw new ForbiddenOperationException();
        }

        var addressBook =
            await customerProfileDao.GetAddressBookAsync(
                currentUser.UserId,
                cancellationToken)
            ?? throw new ResourceNotFoundException(
                "Customer profile");

        var address = addressBook.Addresses
            .SingleOrDefault(item => item.Id == request.Id)
            ?? throw new ResourceNotFoundException(
                "Delivery address");

        return new DeliveryAddressChangeResponse(
            address,
            addressBook.ProfileVersion);
    }
}
