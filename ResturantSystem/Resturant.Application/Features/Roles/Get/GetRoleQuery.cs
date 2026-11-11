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

namespace Restaurant.Application.Features.Roles.Get;

public sealed record GetRoleQuery(Guid Id)
    : IRequest<RolePermissionsResponse>;

public sealed class GetRoleQueryValidator
    : AbstractValidator<GetRoleQuery>
{
    public GetRoleQueryValidator()
    {
        RuleFor(request => request.Id)
            .NotEmpty();
    }
}

public sealed class GetRoleQueryHandler(
    IRolePermissionDao rolePermissionDao)
    : IRequestHandler<GetRoleQuery, RolePermissionsResponse>
{
    public async Task<RolePermissionsResponse> Handle(
        GetRoleQuery request,
        CancellationToken cancellationToken)
    {
        var role =
            await rolePermissionDao.GetRoleDetailsAsync(
                request.Id,
                cancellationToken);

        return role
            ?? throw new ResourceNotFoundException("Role");
    }
}
