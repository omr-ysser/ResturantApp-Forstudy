using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using MediatR;
using Restaurant.Application.Abstractions.Services;
using Restaurant.Application.Common.Models;
using Resturant.Domain.Common;

namespace Restaurant.Application.Features.Clients.CreateMyProfile;

public sealed record CreateMyCustomerProfileCommand(
    string PhoneNumber)
    : IRequest<CustomerProfileResponse>
{
    public override string ToString() =>
        nameof(CreateMyCustomerProfileCommand);
}

public sealed class CreateMyCustomerProfileCommandValidator
    : AbstractValidator<CreateMyCustomerProfileCommand>
{
    public CreateMyCustomerProfileCommandValidator()
    {
        RuleFor(command => command.PhoneNumber)
            .NotEmpty()
            .MaximumLength(
                CustomerRules.MaximumPhoneLength);
    }
}

public sealed class CreateMyCustomerProfileCommandHandler(
    ICustomerProfileService customerProfileService)
    : IRequestHandler<
        CreateMyCustomerProfileCommand,
        CustomerProfileResponse>
{
    public Task<CustomerProfileResponse> Handle(
        CreateMyCustomerProfileCommand request,
        CancellationToken cancellationToken)
    {
        return customerProfileService.CreateMyProfileAsync(
            request.PhoneNumber,
            cancellationToken);
    }
}
