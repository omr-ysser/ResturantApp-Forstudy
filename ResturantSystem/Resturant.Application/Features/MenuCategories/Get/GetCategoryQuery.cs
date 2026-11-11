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

namespace Restaurant.Application.Features.MenuCategories.Get;

public sealed record GetCategoryQuery(Guid Id)
    : IRequest<CategoryResponse>;

public sealed class GetCategoryQueryValidator
    : AbstractValidator<GetCategoryQuery>
{
    public GetCategoryQueryValidator()
    {
        RuleFor(request => request.Id)
            .NotEmpty();
    }
}

public sealed class GetCategoryQueryHandler(
    ICategoryDao categoryDao)
    : IRequestHandler<GetCategoryQuery, CategoryResponse>
{
    public async Task<CategoryResponse> Handle(
        GetCategoryQuery request,
        CancellationToken cancellationToken)
    {
        return await categoryDao.GetAsync(
                   request.Id,
                   cancellationToken)
               ?? throw new ResourceNotFoundException(
                   "Category");
    }
}
