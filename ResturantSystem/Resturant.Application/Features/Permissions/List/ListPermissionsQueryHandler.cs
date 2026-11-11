using MediatR;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Common.Models;

namespace Restaurant.Application.Features.Permissions.List;

public sealed class ListPermissionsQueryHandler(
    IPermissionReadDao permissionReadDao)
    : IRequestHandler<
        ListPermissionsQuery,
        PagedResponse<PermissionResponse>>
{
    public Task<PagedResponse<PermissionResponse>> Handle(
        ListPermissionsQuery request,
        CancellationToken cancellationToken)
    {
        return permissionReadDao.ListAsync(
            request.Page,
            request.PageSize,
            request.Search,
            cancellationToken);
    }
}