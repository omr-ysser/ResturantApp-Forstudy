using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Abstractions.Persistence;
using Restaurant.Application.Common.Models;
using Resturant.Domain.Common;
using Resturant.Domain.Enums;

namespace Resturant.Infrastructure.Payments;

public sealed class PaymentReadDao(
    IAppDbContext dbContext)
    : IPaymentReadDao
{
    public async Task<PagedResponse<PaymentListItemResponse>?>
        ListForOrderAsync(
            Guid orderId,
            Guid? customerScopeUserId,
            int page,
            int pageSize,
            PaymentStatus? status,
            CancellationToken cancellationToken)
    {
        var accessibleOrders = dbContext.Orders
            .AsNoTracking()
            .Where(order => order.Id == orderId);

        if (customerScopeUserId.HasValue)
        {
            var ownerId = customerScopeUserId.Value;

            accessibleOrders = accessibleOrders.Where(order =>
                order.CustomerUserId == ownerId);
        }

        var orderExists = await accessibleOrders.AnyAsync(
            cancellationToken);

        if (!orderExists)
        {
            return null;
        }

        // Keep the ownership scope in the payment query itself,
        // including the count query.
        var payments = dbContext.Payments
            .AsNoTracking()
            .Where(payment =>
                payment.OrderId == orderId &&
                accessibleOrders.Any(order =>
                    order.Id == payment.OrderId));

        if (status.HasValue)
        {
            var requestedStatus = status.Value;

            payments = payments.Where(payment =>
                payment.Status == requestedStatus);
        }

        var totalCount = await payments.CountAsync(
            cancellationToken);

        var rows = await payments
            .OrderByDescending(payment => payment.CreatedAtUtc)
            .ThenBy(payment => payment.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(payment => new
            {
                payment.Id,
                payment.OrderId,
                payment.Amount,
                payment.Method,
                payment.Status,
                payment.CreatedAtUtc,
                payment.PaidAtUtc,
                payment.RefundedAtUtc,

                Version = EF.Property<byte[]>(
                    payment,
                    "RowVersion")
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(payment => new PaymentListItemResponse(
                payment.Id,
                payment.OrderId,
                payment.Amount,
                OrderRules.Currency,
                payment.Method.ToString(),
                payment.Status.ToString(),
                payment.CreatedAtUtc,
                payment.PaidAtUtc,
                payment.RefundedAtUtc,
                Convert.ToBase64String(payment.Version)))
            .ToArray();

        return new PagedResponse<PaymentListItemResponse>(
            page,
            pageSize,
            totalCount,
            items);
    }
}
