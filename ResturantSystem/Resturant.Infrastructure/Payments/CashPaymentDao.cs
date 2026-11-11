using Microsoft.EntityFrameworkCore;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Abstractions.Persistence;
using Restaurant.Application.Common.Models;
using Resturant.Domain.Common;
using Resturant.Domain.Entities;
using Resturant.Domain.Enums;

namespace Resturant.Infrastructure.Payments;

public sealed class CashPaymentDao(
    IAppDbContext dbContext)
    : ICashPaymentDao
{
    public Task<Payment?> FindByRequestKeyAsync(
        Guid orderId,
        Guid requestKey,
        CancellationToken cancellationToken)
    {
        return dbContext.Payments
            .AsTracking()
            .SingleOrDefaultAsync(payment =>
                    payment.OrderId == orderId &&
                    payment.CollectionRequestKey == requestKey,
                cancellationToken);
    }

    public Task<Payment?> GetTrackedForOrderAsync(
        Guid orderId,
        Guid paymentId,
        CancellationToken cancellationToken)
    {
        return dbContext.Payments
            .AsTracking()
            .SingleOrDefaultAsync(payment =>
                    payment.Id == paymentId &&
                    payment.OrderId == orderId,
                cancellationToken);
    }

    public Task<bool> HasBlockingPaymentAsync(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        return dbContext.Payments
            .AsNoTracking()
            .AnyAsync(payment =>
                    payment.OrderId == orderId &&
                    (
                        payment.Status == PaymentStatus.Pending ||
                        payment.Status == PaymentStatus.Paid ||
                        payment.Status == PaymentStatus.Refunded
                    ),
                cancellationToken);
    }

    public byte[] GetVersion(Payment payment)
    {
        var version = dbContext.Payments
            .Entry(payment)
            .Property<byte[]>("RowVersion")
            .CurrentValue;

        if (version is null || version.Length != 8)
        {
            throw new InvalidOperationException(
                "A persisted payment rowversion is required.");
        }

        return version;
    }

    public void SetExpectedVersion(
        Payment payment,
        byte[] version)
    {
        dbContext.Payments
            .Entry(payment)
            .Property<byte[]>("RowVersion")
            .OriginalValue = version;
    }

    public void Add(Payment payment)
    {
        dbContext.Payments.Add(payment);
    }

    public CashPaymentResponse ToResponse(Payment payment)
    {
        return new CashPaymentResponse(
            payment.Id,
            payment.OrderId,
            payment.Amount,
            OrderRules.Currency,
            payment.Method.ToString(),
            payment.Status.ToString(),
            payment.PaidAtUtc,
            payment.CollectedByUserId,
            payment.RefundedAtUtc,
            Convert.ToBase64String(GetVersion(payment)));
    }
}