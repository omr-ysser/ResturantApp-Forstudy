using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Resturant.Domain.Common;
using Resturant.Domain.Exceptions;

namespace Resturant.Domain.Entities;

public sealed class OrderCreationReceipt : BaseEntity
{
    private OrderCreationReceipt()
    {
    }

    internal OrderCreationReceipt(
        Guid orderId,
        Guid userId,
        Guid requestKey,
        string requestHash)
    {
        if (orderId == Guid.Empty)
        {
            throw new DomainException(
                "Order is required.");
        }

        if (userId == Guid.Empty)
        {
            throw new DomainException(
                "Idempotency user is required.");
        }

        if (requestKey == Guid.Empty)
        {
            throw new DomainException(
                "Idempotency key is required.");
        }

        if (!IsValidHash(requestHash))
        {
            throw new DomainException(
                "Request hash must be a 64-character hexadecimal SHA-256 hash.");
        }

        OrderId = orderId;
        UserId = userId;
        RequestKey = requestKey;
        RequestHash = requestHash.ToUpperInvariant();
    }

    public Guid OrderId { get; private set; }

    public Order Order { get; private set; } = null!;

    public Guid UserId { get; private set; }

    public User User { get; private set; } = null!;

    public Guid RequestKey { get; private set; }

    public string RequestHash { get; private set; } = string.Empty;

    private static bool IsValidHash(string? value)
    {
        if (value is null || value.Length != 64)
        {
            return false;
        }

        foreach (var character in value)
        {
            var isHex =
                character is >= '0' and <= '9'
                or >= 'A' and <= 'F'
                or >= 'a' and <= 'f';

            if (!isHex)
            {
                return false;
            }
        }

        return true;
    }
}
