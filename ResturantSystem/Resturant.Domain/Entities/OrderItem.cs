using Resturant.Domain.Common;
using Resturant.Domain.Exceptions;

namespace Resturant.Domain.Entities;

public sealed class OrderItem : BaseEntity
{
    private OrderItem()
    {
    }

    internal OrderItem(
        Guid orderId,
        Guid menuItemId,
        string itemName,
        decimal unitPrice,
        int quantity,
        string? notes)
    {
        if (orderId == Guid.Empty)
        {
            throw new DomainException(
                "Order is required.");
        }

        if (menuItemId == Guid.Empty)
        {
            throw new DomainException(
                "Menu item is required.");
        }

        OrderRules.EnsureValidUnitPrice(unitPrice);

        OrderRules.EnsureValidQuantity(quantity);

        OrderRules.EnsureValidTotal(
            unitPrice * quantity);

        OrderId = orderId;

        MenuItemId = menuItemId;

        ItemName = OrderRules.RequiredText(
            itemName,
            OrderRules.MaximumItemNameLength,
            "Item name");

        UnitPrice = unitPrice;

        Quantity = quantity;

        Notes = OrderRules.OptionalText(
            notes,
            OrderRules.MaximumItemNotesLength,
            "Item notes");
    }

    public Guid OrderId { get; private set; }

    public Order Order { get; private set; } = null!;

    public Guid MenuItemId { get; private set; }

    public MenuItem MenuItem { get; private set; } = null!;

    // Historical snapshot captured when the item is added.
    public string ItemName { get; private set; } = string.Empty;

    public decimal UnitPrice { get; private set; }

    public int Quantity { get; private set; }

    public string? Notes { get; private set; }

    public decimal TotalPrice => UnitPrice * Quantity;

    internal void ChangeQuantity(int quantity)
    {
        OrderRules.EnsureValidQuantity(quantity);

        OrderRules.EnsureValidTotal(
            UnitPrice * quantity);

        Quantity = quantity;

        MarkUpdated();
    }
}