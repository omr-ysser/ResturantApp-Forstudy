using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Abstractions.Persistence;
using Restaurant.Application.Common.Models;
using Resturant.Domain.Entities;
using Resturant.Domain.Enums;

namespace Resturant.Infrastructure.Orders;

public sealed class OrderWorkflowDao(
    IAppDbContext dbContext)
    : IOrderWorkflowDao
{
    public Task<Order?> GetTrackedForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return dbContext.Orders
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM [dbo].[Orders] WITH (UPDLOCK, HOLDLOCK)
                WHERE [Id] = {id}
                """)
            .AsTracking()
            .Include(order => order.Items)
            .Include(order => order.StatusHistory)
            .AsSplitQuery()
            .SingleOrDefaultAsync(cancellationToken);
    }

    public byte[] GetVersion(Order order)
    {
        return dbContext.Orders
            .Entry(order)
            .Property<byte[]>("RowVersion")
            .CurrentValue!;
    }

    public void SetExpectedVersion(
        Order order,
        byte[] version)
    {
        dbContext.Orders
            .Entry(order)
            .Property<byte[]>("RowVersion")
            .OriginalValue = version;
    }

    public Task<bool> HasPaidPaymentsAsync(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        return dbContext.Payments
            .AsNoTracking()
            .AnyAsync(payment =>
                    payment.OrderId == orderId &&
                    payment.Status == PaymentStatus.Paid,
                cancellationToken);
    }

    public Task<bool> HasOtherActiveTableOrdersAsync(
        Guid tableId,
        Guid excludingOrderId,
        CancellationToken cancellationToken)
    {
        return dbContext.Orders
            .AsNoTracking()
            .AnyAsync(order =>
                    order.Id != excludingOrderId &&
                    order.DiningTableId == tableId &&
                    order.Status != OrderStatus.Completed &&
                    order.Status != OrderStatus.Cancelled,
                cancellationToken);
    }

    public async Task<IReadOnlyList<OrderStatusHistoryResponse>?>
        GetHistoryAsync(
            Guid orderId,
            Guid? customerScopeUserId,
            CancellationToken cancellationToken)
    {
        var query = dbContext.Orders
            .AsNoTracking()
            .Where(order => order.Id == orderId);

        if (customerScopeUserId.HasValue)
        {
            var ownerId = customerScopeUserId.Value;

            query = query.Where(order =>
                order.CustomerUserId == ownerId);
        }

        var exists = await query.AnyAsync(
            cancellationToken);

        if (!exists)
        {
            return null;
        }

        var rows = await query
            .SelectMany(order => order.StatusHistory)
            .OrderBy(entry => entry.OccurredAtUtc)
            .ThenBy(entry => entry.CreatedAtUtc)
            .ThenBy(entry => entry.Id)
            .Select(entry => new
            {
                entry.Id,
                entry.FromStatus,
                entry.ToStatus,
                entry.ActorUserId,
                entry.OccurredAtUtc,
                entry.Reason
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(entry => new OrderStatusHistoryResponse(
                entry.Id,
                entry.FromStatus?.ToString(),
                entry.ToStatus.ToString(),
                entry.ActorUserId,
                entry.OccurredAtUtc,
                entry.Reason))
            .ToArray();
    }
}
