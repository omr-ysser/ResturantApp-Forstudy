using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using MediatR;
using Restaurant.Application.Abstractions.Services;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Common.Security;

namespace Restaurant.Application.Features.Clients.Addresses;

public sealed record DeleteMyDeliveryAddressCommand(
    Guid Id,
    string ProfileVersion)
    : IRequest<DeliveryAddressBookResponse>;

public sealed class DeleteMyDeliveryAddressCommandValidator
    : AbstractValidator<DeleteMyDeliveryAddressCommand>
{
    public DeleteMyDeliveryAddressCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.ProfileVersion)
            .Must(RowVersionFormat.IsValid)
            .WithMessage(
                "ProfileVersion must be a valid Base64 SQL Server rowversion.");
    }
}

public sealed class DeleteMyDeliveryAddressCommandHandler(
    ICustomerProfileService customerProfileService)
    : IRequestHandler<
        DeleteMyDeliveryAddressCommand,
        DeliveryAddressBookResponse>
{
    public Task<DeliveryAddressBookResponse> Handle(
        DeleteMyDeliveryAddressCommand request,
        CancellationToken cancellationToken)
    {
        return customerProfileService.DeleteMyAddressAsync(
            request.Id,
            request.ProfileVersion,
            cancellationToken);
    }
}
