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

namespace Restaurant.Application.Features.Users.Get;

public sealed record GetUserQuery(Guid Id)
    : IRequest<UserAccessResponse>;

public sealed class GetUserQueryValidator
    : AbstractValidator<GetUserQuery>
{
    public GetUserQueryValidator()
    {
        RuleFor(request => request.Id)
            .NotEmpty();
    }
}

public sealed class GetUserQueryHandler(
    IUserAccessDao userDao)
    : IRequestHandler<GetUserQuery, UserAccessResponse>
{
    public async Task<UserAccessResponse> Handle(
        GetUserQuery request,
        CancellationToken cancellationToken)
    {
        var user = await userDao.GetDetailsAsync(
            request.Id,
            cancellationToken);

        return user
            ?? throw new ResourceNotFoundException("User");
    }
}
