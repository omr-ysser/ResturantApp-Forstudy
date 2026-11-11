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

namespace Restaurant.Application.Features.Clients.UpdateMyProfile;

public sealed record UpdateMyCustomerProfileCommand(
    string PhoneNumber,
    string Version)
    : IRequest<CustomerProfileResponse>
{
    public override string ToString() =>
        nameof(UpdateMyCustomerProfileCommand);
}

public sealed class UpdateMyCustomerProfileCommandValidator
    : AbstractValidator<UpdateMyCustomerProfileCommand>
{
    public UpdateMyCustomerProfileCommandValidator()
    {
        RuleFor(command => command.PhoneNumber)
            .NotEmpty()
            .MaximumLength(
                CustomerRules.MaximumPhoneLength);

        RuleFor(command => command.Version)
            .Must(RowVersionFormat.IsValid)
            .WithMessage(
                "Version must be a valid Base64 SQL Server rowversion.");
    }
}

public sealed class UpdateMyCustomerProfileCommandHandler(
    ICustomerProfileService customerProfileService)
    : IRequestHandler<
        UpdateMyCustomerProfileCommand,
        CustomerProfileResponse>
{
    public Task<CustomerProfileResponse> Handle(
        UpdateMyCustomerProfileCommand request,
        CancellationToken cancellationToken)
    {
        return customerProfileService.UpdateMyProfileAsync(
            request.PhoneNumber,
            request.Version,
            cancellationToken);
    }
}
