using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Common.Models;

public sealed record CreateMyCustomerProfileRequest(
    string PhoneNumber)
{
    public override string ToString() =>
        nameof(CreateMyCustomerProfileRequest);
}

public sealed record UpdateMyCustomerProfileRequest(
    string PhoneNumber,
    string Version)
{
    public override string ToString() =>
        nameof(UpdateMyCustomerProfileRequest);
}

public sealed record CustomerProfileResponse(
    Guid Id,
    Guid UserId,
    string FullName,
    string Email,
    string PhoneNumber,
    int AddressCount,
    string Version);
