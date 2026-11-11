using Microsoft.Extensions.Logging;
using Restaurant.Application.Abstractions.Authentication;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Abstractions.Persistence;
using Restaurant.Application.Abstractions.Services;
using Restaurant.Application.Common.Exceptions;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Common.Security;
using Restaurant.Application.Features.Payments;
using Resturant.Domain.Common;
using Resturant.Domain.Entities;
using Resturant.Domain.Enums;

namespace Restaurant.Application.Features.Payments.Services;

public sealed class CashPaymentService(
    ICashPaymentDao cashPaymentDao,
    IOrderWorkflowDao orderWorkflowDao,
    IAuthenticationDao authenticationDao,
    IAuthenticationSessionDao sessionDao,
    IAppDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    ILogger<CashPaymentService> logger)
    : ICashPaymentService
{
    public async Task<CashCollectionResponse> CollectAsync(
        Guid orderId,
        Guid requestKey,
        string orderVersion,
        CancellationToken cancellationToken)
    {
        var expectedVersion = ParseVersion(
            orderVersion,
            "order");

        if (requestKey == Guid.Empty)
        {
            throw new BusinessConflictException(
                "A collection idempotency key is required.");
        }

        await using var transaction =
            await dbContext.BeginTransactionAsync(
                cancellationToken);

        var actorId = await GetAuthorizedActorAsync(
            PaymentPermissions.CollectCash,
            cancellationToken);

        var order =
            await orderWorkflowDao.GetTrackedForUpdateAsync(
                orderId,
                cancellationToken);

        if (order is null)
        {
            throw new ResourceNotFoundException("Order");
        }

        // Check replay before current order state and version.
        var existingPayment =
            await cashPaymentDao.FindByRequestKeyAsync(
                order.Id,
                requestKey,
                cancellationToken);

        if (existingPayment is not null)
        {
            if (existingPayment.Method != PaymentMethod.Cash)
            {
                throw new BusinessConflictException(
                    "The collection key belongs to a different payment method.");
            }

            var existingResponse =
                cashPaymentDao.ToResponse(existingPayment);

            await transaction.CommitAsync(
                cancellationToken);

            logger.LogInformation(
                "Cash collection {PaymentId} replayed for order {OrderId} by user {UserId}",
                existingPayment.Id,
                order.Id,
                actorId);

            return new CashCollectionResponse(
                existingResponse,
                Replayed: true);
        }

        var actualVersion =
            orderWorkflowDao.GetVersion(order);

        if (!actualVersion.AsSpan().SequenceEqual(
                expectedVersion))
        {
            throw new BusinessConflictException(
                "The order has changed. Reload it before collecting payment.");
        }

        if (order.Status is not (
                OrderStatus.Confirmed or
                OrderStatus.Preparing or
                OrderStatus.Ready or
                OrderStatus.Completed))
        {
            throw new BusinessConflictException(
                "Cash can only be collected for confirmed, preparing, " +
                "ready, or completed orders.");
        }

        if (await cashPaymentDao.HasBlockingPaymentAsync(
                order.Id,
                cancellationToken))
        {
            throw new BusinessConflictException(
                "The order already has a pending, paid, or refunded payment. " +
                "Another cash collection cannot be recorded.");
        }

        var amount = order.TotalAmount;

        OrderRules.EnsureValidTotal(amount);

        var payment = new Payment(
            order.Id,
            amount,
            PaymentMethod.Cash);

        payment.MarkPaid(
            actorId,
            timeProvider.GetUtcNow(),
            requestKey);

        cashPaymentDao.Add(payment);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        var response = cashPaymentDao.ToResponse(payment);

        await transaction.CommitAsync(
            cancellationToken);

        logger.LogInformation(
            "Cash payment {PaymentId} recorded for order {OrderId} by user {UserId}",
            payment.Id,
            order.Id,
            actorId);

        return new CashCollectionResponse(
            response,
            Replayed: false);
    }

    public async Task<CashPaymentResponse> RefundAsync(
        Guid orderId,
        Guid paymentId,
        string paymentVersion,
        string reason,
        CancellationToken cancellationToken)
    {
        var expectedVersion = ParseVersion(
            paymentVersion,
            "payment");

        if (string.IsNullOrWhiteSpace(reason) ||
            reason.Length > 500)
        {
            throw new BusinessConflictException(
                "A refund reason of at most 500 characters is required.");
        }

        await using var transaction =
            await dbContext.BeginTransactionAsync(
                cancellationToken);

        var actorId = await GetAuthorizedActorAsync(
            PaymentPermissions.RefundCash,
            cancellationToken);

        // Lock the order before accessing its payments.
        // Collection and cancellation use the same order lock.
        var order =
            await orderWorkflowDao.GetTrackedForUpdateAsync(
                orderId,
                cancellationToken);

        if (order is null)
        {
            throw new ResourceNotFoundException("Order");
        }

        var payment =
            await cashPaymentDao.GetTrackedForOrderAsync(
                order.Id,
                paymentId,
                cancellationToken);

        if (payment is null)
        {
            throw new ResourceNotFoundException("Payment");
        }

        var actualVersion =
            cashPaymentDao.GetVersion(payment);

        if (!actualVersion.AsSpan().SequenceEqual(
                expectedVersion))
        {
            throw new BusinessConflictException(
                "The payment has changed. Reload it before recording a refund.");
        }

        if (payment.Method != PaymentMethod.Cash)
        {
            throw new BusinessConflictException(
                "Only cash payments can be refunded through this endpoint.");
        }

        if (payment.Status != PaymentStatus.Paid)
        {
            throw new BusinessConflictException(
                "Only paid payments can be refunded. " +
                "The payment may already have been refunded.");
        }

        if (!payment.PaidAtUtc.HasValue)
        {
            throw new BusinessConflictException(
                "The historical payment requires reconciliation " +
                "before a refund can be recorded.");
        }

        cashPaymentDao.SetExpectedVersion(
            payment,
            expectedVersion);

        payment.MarkRefunded(
            actorId,
            timeProvider.GetUtcNow(),
            reason);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        var response = cashPaymentDao.ToResponse(payment);

        await transaction.CommitAsync(
            cancellationToken);

        logger.LogInformation(
            "Cash payment {PaymentId} refunded for order {OrderId} by user {UserId}",
            payment.Id,
            order.Id,
            actorId);

        return response;
    }

    private async Task<Guid> GetAuthorizedActorAsync(
        string requiredPermission,
        CancellationToken cancellationToken)
    {
        var actorId = currentUser.UserId;

        // This method must run inside the workflow transaction.
        await sessionDao.LockUserAsync(
            actorId,
            cancellationToken);

        var access = await authenticationDao.GetAccessAsync(
            actorId,
            cancellationToken);

        if (access is null ||
            !await sessionDao.IsSessionActiveAsync(
                actorId,
                currentUser.SessionId,
                timeProvider.GetUtcNow(),
                cancellationToken))
        {
            throw new AuthenticationFailedException();
        }

        if (!access.Permissions.Contains(
                requiredPermission,
                StringComparer.Ordinal))
        {
            throw new ForbiddenOperationException();
        }

        return actorId;
    }

    private static byte[] ParseVersion(
        string version,
        string resource)
    {
        if (!RowVersionFormat.IsValid(version))
        {
            throw new BusinessConflictException(
                $"The {resource} version is invalid. Reload the {resource}.");
        }

        return Convert.FromBase64String(version);
    }
}