using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using MediatR;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Common.Exceptions;
using Restaurant.Application.Common.Models;

namespace Restaurant.Application.Features.Permissions.Get;

public sealed record GetPermissionQuery(Guid Id)
    : IRequest<PermissionDetailsResponse>;

public sealed class GetPermissionQueryValidator
    : AbstractValidator<GetPermissionQuery>
{
    public GetPermissionQueryValidator()
    {
        RuleFor(request => request.Id)
            .NotEmpty();
    }
}

public sealed class GetPermissionQueryHandler(
    IPermissionManagementDao permissionDao)
    : IRequestHandler<
        GetPermissionQuery,
        PermissionDetailsResponse>
{
    public async Task<PermissionDetailsResponse> Handle(
        GetPermissionQuery request,
        CancellationToken cancellationToken)
    {
        var permission = await permissionDao.GetDetailsAsync(
            request.Id,
            cancellationToken);

        return permission
            ?? throw new ResourceNotFoundException(
                "Permission");
    }
}
