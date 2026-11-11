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

public sealed record UpdateMyDeliveryAddressCommand(
    Guid Id,
    string ProfileVersion,
    DeliveryAddressInput Address)
    : IRequest<DeliveryAddressChangeResponse>
{
    public override string ToString() =>
        nameof(UpdateMyDeliveryAddressCommand);
}

public sealed class UpdateMyDeliveryAddressCommandValidator
    : AbstractValidator<UpdateMyDeliveryAddressCommand>
{
    public UpdateMyDeliveryAddressCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.ProfileVersion)
            .Must(RowVersionFormat.IsValid)
            .WithMessage(
                "ProfileVersion must be a valid Base64 SQL Server rowversion.");

        RuleFor(command => command.Address)
            .NotNull()
            .SetValidator(
                new DeliveryAddressInputValidator());
    }
}

public sealed class UpdateMyDeliveryAddressCommandHandler(
    ICustomerProfileService customerProfileService)
    : IRequestHandler<
        UpdateMyDeliveryAddressCommand,
        DeliveryAddressChangeResponse>
{
    public Task<DeliveryAddressChangeResponse> Handle(
        UpdateMyDeliveryAddressCommand request,
        CancellationToken cancellationToken)
    {
        return customerProfileService.UpdateMyAddressAsync(
            request.Id,
            request.Address,
            request.ProfileVersion,
            cancellationToken);
    }
}
