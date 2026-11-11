using Restaurant.Application.Common.Models;
using Resturant.Domain.Entities;

namespace Restaurant.Application.Abstractions.Dao;

public interface ICashPaymentDao
{
    Task<Payment?> FindByRequestKeyAsync(
        Guid orderId,
        Guid requestKey,
        CancellationToken cancellationToken);

    Task<Payment?> GetTrackedForOrderAsync(
        Guid orderId,
        Guid paymentId,
        CancellationToken cancellationToken);

    Task<bool> HasBlockingPaymentAsync(
        Guid orderId,
        CancellationToken cancellationToken);

    byte[] GetVersion(Payment payment);

    void SetExpectedVersion(
        Payment payment,
        byte[] version);

    void Add(Payment payment);

    CashPaymentResponse ToResponse(Payment payment);
}