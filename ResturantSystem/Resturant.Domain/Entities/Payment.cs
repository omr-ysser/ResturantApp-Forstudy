using Resturant.Domain.Common;
using Resturant.Domain.Enums;
using Resturant.Domain.Exceptions;

namespace Resturant.Domain.Entities;

public sealed class Payment : BaseEntity
{
    private Payment()
    {
    }

    public Payment(
        Guid orderId,
        decimal amount,
        PaymentMethod method)
    {
        if (orderId == Guid.Empty)
        {
            throw new DomainException(
                "Order is required.");
        }

        if (amount <= 0 ||
            amount > OrderRules.MaximumMoneyAmount ||
            decimal.Round(amount, 2) != amount)
        {
            throw new DomainException(
                "Payment amount must be positive, fit decimal(18,2), " +
                "and contain at most two decimal places.");
        }

        if (!Enum.IsDefined(typeof(PaymentMethod), method))
        {
            throw new DomainException(
                "Invalid payment method.");
        }

        OrderId = orderId;
        Amount = amount;
        Method = method;
    }

    public Guid OrderId { get; private set; }

    public Order Order { get; private set; } = null!;

    public decimal Amount { get; private set; }

    public PaymentMethod Method { get; private set; }

    public PaymentStatus Status { get; private set; }
        = PaymentStatus.Pending;

    public string? TransactionReference { get; private set; }

    public string? FailureReason { get; private set; }

    public DateTimeOffset? PaidAtUtc { get; private set; }

    public DateTimeOffset? RefundedAtUtc { get; private set; }

    // Nullable to preserve existing payment records
    // without inventing historical audit information.
    public Guid? CollectedByUserId { get; private set; }

    public User? CollectedByUser { get; private set; }

    public Guid? RefundedByUserId { get; private set; }

    public User? RefundedByUser { get; private set; }

    public string? RefundReason { get; private set; }

    public Guid? CollectionRequestKey { get; private set; }

    public void MarkPaid(
        Guid actorUserId,
        DateTimeOffset paidAtUtc,
        Guid requestKey)
    {
        EnsurePending();

        EnsureCashMethod();

        EnsureActor(actorUserId);

        if (requestKey == Guid.Empty)
        {
            throw new DomainException(
                "Collection idempotency key is required.");
        }

        var normalizedTime = ValidateTimestamp(
            paidAtUtc,
            CreatedAtUtc,
            "Collection");

        CollectedByUserId = actorUserId;
        CollectionRequestKey = requestKey;
        PaidAtUtc = normalizedTime;

        Status = PaymentStatus.Paid;

        MarkUpdated();
    }

    public void MarkFailed(string reason)
    {
        EnsurePending();

        FailureReason = OrderRules.RequiredText(
            reason,
            maximumLength: 1000,
            fieldName: "Failure reason");

        Status = PaymentStatus.Failed;

        MarkUpdated();
    }

    public void MarkRefunded(
        Guid actorUserId,
        DateTimeOffset refundedAtUtc,
        string reason)
    {
        if (Status != PaymentStatus.Paid)
        {
            throw new DomainException(
                "Only paid payments can be refunded.");
        }

        EnsureCashMethod();

        EnsureActor(actorUserId);

        if (!PaidAtUtc.HasValue)
        {
            throw new DomainException(
                "The payment collection timestamp must be reconciled " +
                "before recording a refund.");
        }

        var normalizedReason = OrderRules.RequiredText(
            reason,
            maximumLength: 500,
            fieldName: "Refund reason");

        var normalizedTime = ValidateTimestamp(
            refundedAtUtc,
            PaidAtUtc.Value,
            "Refund");

        RefundedByUserId = actorUserId;
        RefundedAtUtc = normalizedTime;
        RefundReason = normalizedReason;

        Status = PaymentStatus.Refunded;

        MarkUpdated();
    }

    private void EnsurePending()
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new DomainException(
                "Payment is no longer pending.");
        }
    }

    private void EnsureCashMethod()
    {
        if (Method != PaymentMethod.Cash)
        {
            throw new DomainException(
                "Only cash collection and cash refunds " +
                "are supported by this workflow.");
        }
    }

    private static void EnsureActor(Guid actorUserId)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new DomainException(
                "Payment operation actor is required.");
        }
    }

    private static DateTimeOffset ValidateTimestamp(
        DateTimeOffset timestamp,
        DateTimeOffset minimumTimestamp,
        string operation)
    {
        if (timestamp == default)
        {
            throw new DomainException(
                $"{operation} timestamp is required.");
        }

        var utc = timestamp.ToUniversalTime();

        if (utc < minimumTimestamp.ToUniversalTime())
        {
            throw new DomainException(
                $"{operation} timestamp cannot precede " +
                "the previous payment event.");
        }

        return utc;
    }
}