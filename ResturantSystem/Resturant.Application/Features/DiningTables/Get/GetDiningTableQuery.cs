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

namespace Restaurant.Application.Features.DiningTables.Get;

public sealed record GetDiningTableQuery(Guid Id)
    : IRequest<DiningTableResponse>;

public sealed class GetDiningTableQueryValidator
    : AbstractValidator<GetDiningTableQuery>
{
    public GetDiningTableQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty();
    }
}

public sealed class GetDiningTableQueryHandler(
    IDiningTableDao diningTableDao)
    : IRequestHandler<
        GetDiningTableQuery,
        DiningTableResponse>
{
    public async Task<DiningTableResponse> Handle(
        GetDiningTableQuery request,
        CancellationToken cancellationToken)
    {
        return await diningTableDao.GetAsync(
                   request.Id,
                   cancellationToken)
               ?? throw new ResourceNotFoundException("Dining table");
    }
}
