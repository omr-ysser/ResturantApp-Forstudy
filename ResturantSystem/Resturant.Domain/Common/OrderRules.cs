using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Resturant.Domain.Exceptions;

namespace Resturant.Domain.Common;

public static class OrderRules
{
    public const string Currency = "EGP";

    public const int MaximumItemQuantity = 99;

    public const int MaximumOrderLines = 50;

    public const int MaximumOrderNumberLength = 50;

    public const int MaximumCustomerNameLength = 150;

    public const int MaximumCustomerPhoneLength = 30;

    public const int MaximumDeliveryAddressLength = 500;

    public const int MaximumNotesLength = 1000;

    public const int MaximumCancellationReasonLength = 500;

    public const int MaximumItemNameLength = 150;

    public const int MaximumItemNotesLength = 500;

    public const decimal MaximumMoneyAmount =
        9_999_999_999_999_999.99m;

    public static void EnsureValidQuantity(int quantity)
    {
        if (quantity < 1 ||
            quantity > MaximumItemQuantity)
        {
            throw new DomainException(
                $"Quantity must be between 1 and {MaximumItemQuantity}.");
        }
    }

    public static void EnsureValidUnitPrice(decimal unitPrice)
    {
        if (unitPrice <= 0 ||
            unitPrice > MaximumMoneyAmount ||
            decimal.Round(unitPrice, 2) != unitPrice)
        {
            throw new DomainException(
                "Unit price must be positive, fit decimal(18,2), " +
                "and contain at most two decimal places.");
        }
    }

    public static void EnsureValidTotal(decimal total)
    {
        if (total <= 0 ||
            total > MaximumMoneyAmount ||
            decimal.Round(total, 2) != total)
        {
            throw new DomainException(
                "Order total must be positive and fit decimal(18,2).");
        }
    }

    public static string RequiredText(
        string? value,
        int maximumLength,
        string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(
                $"{fieldName} is required.");
        }

        var trimmed = value.Trim();

        if (trimmed.Length > maximumLength)
        {
            throw new DomainException(
                $"{fieldName} must not exceed {maximumLength} characters.");
        }

        return trimmed;
    }

    public static string? OptionalText(
        string? value,
        int maximumLength,
        string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();

        if (trimmed.Length > maximumLength)
        {
            throw new DomainException(
                $"{fieldName} must not exceed {maximumLength} characters.");
        }

        return trimmed;
    }
}
