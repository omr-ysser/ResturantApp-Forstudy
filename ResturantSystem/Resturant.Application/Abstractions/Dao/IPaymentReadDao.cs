using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Restaurant.Application.Common.Models;
using Resturant.Domain.Enums;

namespace Restaurant.Application.Abstractions.Dao;

public interface IPaymentReadDao
{
    Task<PagedResponse<PaymentListItemResponse>?> ListForOrderAsync(
        Guid orderId,
        Guid? customerScopeUserId,
        int page,
        int pageSize,
        PaymentStatus? status,
        CancellationToken cancellationToken);
}
