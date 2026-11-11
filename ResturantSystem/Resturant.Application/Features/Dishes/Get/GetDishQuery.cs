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

namespace Restaurant.Application.Features.Dishes.Get;

public sealed record GetDishQuery(Guid Id)
    : IRequest<DishResponse>;

public sealed class GetDishQueryValidator
    : AbstractValidator<GetDishQuery>
{
    public GetDishQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty();
    }
}

public sealed class GetDishQueryHandler(
    IDishReadDao dishReadDao)
    : IRequestHandler<GetDishQuery, DishResponse>
{
    public async Task<DishResponse> Handle(
        GetDishQuery request,
        CancellationToken cancellationToken)
    {
        var dish = await dishReadDao.GetAsync(
            request.Id,
            cancellationToken);

        return dish
            ?? throw new ResourceNotFoundException("Dish");
    }
}
