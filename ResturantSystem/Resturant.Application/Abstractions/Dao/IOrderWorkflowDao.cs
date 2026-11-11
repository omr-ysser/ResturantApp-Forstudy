using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Restaurant.Application.Common.Models;
using Resturant.Domain.Entities;

namespace Restaurant.Application.Abstractions.Dao;

public interface IOrderWorkflowDao
{
    Task<Order?> GetTrackedForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken);

    byte[] GetVersion(Order order);

    void SetExpectedVersion(
        Order order,
        byte[] version);

    Task<bool> HasPaidPaymentsAsync(
        Guid orderId,
        CancellationToken cancellationToken);

    Task<bool> HasOtherActiveTableOrdersAsync(
        Guid tableId,
        Guid excludingOrderId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OrderStatusHistoryResponse>?> GetHistoryAsync(
        Guid orderId,
        Guid? customerScopeUserId,
        CancellationToken cancellationToken);
}
