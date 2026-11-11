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
using Resturant.Domain.Common;

namespace Restaurant.Application.Features.Clients.Addresses;

public sealed record AddMyDeliveryAddressCommand(
    string ProfileVersion,
    DeliveryAddressInput Address)
    : IRequest<DeliveryAddressChangeResponse>
{
    public override string ToString() =>
        nameof(AddMyDeliveryAddressCommand);
}

public sealed class DeliveryAddressInputValidator
    : AbstractValidator<DeliveryAddressInput>
{
    public DeliveryAddressInputValidator()
    {
        RuleFor(address => address.Label)
            .NotEmpty()
            .MaximumLength(
                CustomerRules.MaximumAddressLabelLength);

        RuleFor(address => address.RecipientName)
            .NotEmpty()
            .MaximumLength(
                CustomerRules.MaximumRecipientNameLength);

        RuleFor(address => address.RecipientPhone)
            .NotEmpty()
            .MaximumLength(
                CustomerRules.MaximumPhoneLength);

        RuleFor(address => address.City)
            .NotEmpty()
            .MaximumLength(
                CustomerRules.MaximumCityLength);

        RuleFor(address => address.District)
            .MaximumLength(
                CustomerRules.MaximumDistrictLength);

        RuleFor(address => address.StreetAddress)
            .NotEmpty()
            .MaximumLength(
                CustomerRules.MaximumStreetAddressLength);

        RuleFor(address => address.BuildingDetails)
            .MaximumLength(
                CustomerRules.MaximumBuildingDetailsLength);

        RuleFor(address => address.DeliveryInstructions)
            .MaximumLength(
                CustomerRules.MaximumDeliveryInstructionsLength);
    }
}

public sealed class AddMyDeliveryAddressCommandValidator
    : AbstractValidator<AddMyDeliveryAddressCommand>
{
    public AddMyDeliveryAddressCommandValidator()
    {
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

public sealed class AddMyDeliveryAddressCommandHandler(
    ICustomerProfileService customerProfileService)
    : IRequestHandler<
        AddMyDeliveryAddressCommand,
        DeliveryAddressChangeResponse>
{
    public Task<DeliveryAddressChangeResponse> Handle(
        AddMyDeliveryAddressCommand request,
        CancellationToken cancellationToken)
    {
        return customerProfileService.AddMyAddressAsync(
            request.Address,
            request.ProfileVersion,
            cancellationToken);
    }
}
