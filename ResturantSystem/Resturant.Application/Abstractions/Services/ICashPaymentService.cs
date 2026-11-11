using Restaurant.Application.Common.Models;

namespace Restaurant.Application.Abstractions.Services;

public interface ICashPaymentService
{
    Task<CashCollectionResponse> CollectAsync(
        Guid orderId,
        Guid requestKey,
        string orderVersion,
        CancellationToken cancellationToken);

    Task<CashPaymentResponse> RefundAsync(
        Guid orderId,
        Guid paymentId,
        string paymentVersion,
        string reason,
        CancellationToken cancellationToken);
}