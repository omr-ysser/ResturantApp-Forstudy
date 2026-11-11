using Resturant.Domain.Common;
using Resturant.Domain.Enums;
using Resturant.Domain.Exceptions;

namespace Resturant.Domain.Entities;

public sealed class Order : BaseEntity
{
    private readonly List<OrderItem> _items = [];

    private readonly List<OrderStatusHistory> _statusHistory = [];

    private Order()
    {
    }

    public Order(
        string orderNumber,
        OrderType type,
        Guid createdByUserId,
        Guid? diningTableId = null,
        string? customerName = null,
        string? customerPhone = null,
        string? deliveryAddress = null,
        string? notes = null,
        Guid? customerUserId = null)
    {
        if (!Enum.IsDefined(typeof(OrderType), type))
        {
            throw new DomainException("Invalid order type.");
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new DomainException("Order creator is required.");
        }

        if (customerUserId.HasValue &&
            customerUserId.Value == Guid.Empty)
        {
            throw new DomainException(
                "Customer account identifier is invalid.");
        }

        if (type == OrderType.DineIn &&
            (!diningTableId.HasValue ||
             diningTableId.Value == Guid.Empty))
        {
            throw new DomainException(
                "Dine-in orders require a table.");
        }

        if (type != OrderType.DineIn &&
            diningTableId.HasValue)
        {
            throw new DomainException(
                "Only dine-in orders can have a table.");
        }

        var normalizedCustomerName = OrderRules.OptionalText(
            customerName,
            OrderRules.MaximumCustomerNameLength,
            "Customer name");

        var normalizedCustomerPhone = OrderRules.OptionalText(
            customerPhone,
            OrderRules.MaximumCustomerPhoneLength,
            "Customer phone");

        var normalizedDeliveryAddress = OrderRules.OptionalText(
            deliveryAddress,
            OrderRules.MaximumDeliveryAddressLength,
            "Delivery address");

        if (type == OrderType.Delivery &&
            (
                normalizedCustomerName is null ||
                normalizedCustomerPhone is null ||
                normalizedDeliveryAddress is null
            ))
        {
            throw new DomainException(
                "Delivery orders require customer name, phone, and address.");
        }

        if (type != OrderType.Delivery &&
            normalizedDeliveryAddress is not null)
        {
            throw new DomainException(
                "Only delivery orders can have a delivery address.");
        }

        OrderNumber = OrderRules.RequiredText(
            orderNumber,
            OrderRules.MaximumOrderNumberLength,
            "Order number");

        Type = type;
        CreatedByUserId = createdByUserId;
        CustomerUserId = customerUserId;
        DiningTableId = diningTableId;
        CustomerName = normalizedCustomerName;
        CustomerPhone = normalizedCustomerPhone;
        DeliveryAddress = normalizedDeliveryAddress;

        Notes = OrderRules.OptionalText(
            notes,
            OrderRules.MaximumNotesLength,
            "Order notes");

        _statusHistory.Add(
            new OrderStatusHistory(
                Id,
                fromStatus: null,
                toStatus: OrderStatus.Pending,
                actorUserId: createdByUserId,
                occurredAtUtc: CreatedAtUtc));
    }

    public string OrderNumber { get; private set; } = string.Empty;

    public OrderType Type { get; private set; }

    public OrderStatus Status { get; private set; }
        = OrderStatus.Pending;

    public Guid CreatedByUserId { get; private set; }

    public User CreatedByUser { get; private set; } = null!;

    public Guid? CustomerUserId { get; private set; }

    public User? CustomerUser { get; private set; }

    public Guid? DiningTableId { get; private set; }

    public DiningTable? DiningTable { get; private set; }

    public Guid? ReservationId { get; private set; }

    public Reservation? Reservation { get; private set; }

    public DateTimeOffset? DiningStartedAtUtc { get; private set; }

    public DateTimeOffset? ExpectedDepartureAtUtc { get; private set; }

    public string? CustomerName { get; private set; }

    public string? CustomerPhone { get; private set; }

    public string? DeliveryAddress { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public string? CancellationReason { get; private set; }

    public OrderCreationReceipt? CreationReceipt { get; private set; }

    public IReadOnlyCollection<OrderItem> Items =>
        _items.AsReadOnly();

    public IReadOnlyCollection<OrderStatusHistory> StatusHistory =>
        _statusHistory.AsReadOnly();

    public decimal TotalAmount =>
        _items.Sum(item => item.TotalPrice);

    public void PlanWalkInVisit(DateTimeOffset startedAtUtc)
    {
        EnsurePending();

        if (Type != OrderType.DineIn ||
            !DiningTableId.HasValue ||
            ReservationId.HasValue)
        {
            throw new DomainException(
                "A walk-in visit plan requires a dine-in order " +
                "without a reservation.");
        }

        if (DiningStartedAtUtc.HasValue ||
            ExpectedDepartureAtUtc.HasValue)
        {
            throw new DomainException(
                "The walk-in visit has already been planned.");
        }

        if (startedAtUtc == default)
        {
            throw new DomainException(
                "Dining start timestamp is required.");
        }

        var start = startedAtUtc.ToUniversalTime();

        if (start > DateTimeOffset.MaxValue.Subtract(
                DiningOccupancyRules.DefaultWalkInDuration))
        {
            throw new DomainException(
                "Dining start timestamp is outside the supported range.");
        }

        DiningStartedAtUtc = start;

        ExpectedDepartureAtUtc = start.Add(
            DiningOccupancyRules.DefaultWalkInDuration);

        MarkUpdated();
    }

    public void AttachReservation(Reservation reservation)
    {
        ArgumentNullException.ThrowIfNull(reservation);

        EnsurePending();

        if (Type != OrderType.DineIn)
        {
            throw new DomainException(
                "Only dine-in orders can be linked to a reservation.");
        }

        if (ReservationId.HasValue)
        {
            throw new DomainException(
                "The order is already linked to a reservation.");
        }

        if (DiningStartedAtUtc.HasValue ||
            ExpectedDepartureAtUtc.HasValue)
        {
            throw new DomainException(
                "An order with a walk-in visit plan cannot " +
                "be linked to a reservation.");
        }

        if (reservation.Status != ReservationStatus.Seated)
        {
            throw new DomainException(
                "The reservation must be seated before creating its order.");
        }

        if (DiningTableId != reservation.DiningTableId)
        {
            throw new DomainException(
                "The order and reservation must use the same dining table.");
        }

        if (CustomerUserId != reservation.CustomerUserId)
        {
            throw new DomainException(
                "The order and reservation must have the same customer owner.");
        }

        ReservationId = reservation.Id;
        Reservation = reservation;

        MarkUpdated();
    }

    public void AddItem(
        MenuItem menuItem,
        int quantity,
        string? notes = null)
    {
        ArgumentNullException.ThrowIfNull(menuItem);

        EnsurePending();

        if (!menuItem.IsAvailable)
        {
            throw new DomainException(
                "Menu item is unavailable.");
        }

        if (_items.Count >= OrderRules.MaximumOrderLines)
        {
            throw new DomainException(
                $"An order cannot contain more than " +
                $"{OrderRules.MaximumOrderLines} lines.");
        }

        OrderRules.EnsureValidQuantity(quantity);
        OrderRules.EnsureValidUnitPrice(menuItem.Price);

        OrderRules.EnsureValidTotal(
            TotalAmount + menuItem.Price * quantity);

        _items.Add(
            new OrderItem(
                Id,
                menuItem.Id,
                menuItem.Name,
                menuItem.Price,
                quantity,
                notes));

        MarkUpdated();
    }

    public void ChangeItemQuantity(
        Guid orderItemId,
        int quantity)
    {
        EnsurePending();

        OrderRules.EnsureValidQuantity(quantity);

        var item = FindItem(orderItemId);

        OrderRules.EnsureValidTotal(
            TotalAmount -
            item.TotalPrice +
            item.UnitPrice * quantity);

        item.ChangeQuantity(quantity);

        MarkUpdated();
    }

    public void RemoveItem(Guid orderItemId)
    {
        EnsurePending();

        var item = FindItem(orderItemId);

        if (_items.Count == 1)
        {
            throw new DomainException(
                "The last order item cannot be removed. " +
                "Cancel the order instead.");
        }

        _items.Remove(item);

        MarkUpdated();
    }

    public void RegisterCreationReceipt(
        Guid requestKey,
        string requestHash)
    {
        EnsurePending();
        EnsureReadyForPersistence();

        if (CreationReceipt is not null)
        {
            throw new DomainException(
                "The order already has an idempotency receipt.");
        }

        CreationReceipt = new OrderCreationReceipt(
            Id,
            CreatedByUserId,
            requestKey,
            requestHash);

        MarkUpdated();
    }

    public void EnsureReadyForPersistence()
    {
        if (_items.Count == 0)
        {
            throw new DomainException(
                "An order must contain at least one item.");
        }

        if (_items.Count > OrderRules.MaximumOrderLines)
        {
            throw new DomainException(
                $"An order cannot contain more than " +
                $"{OrderRules.MaximumOrderLines} lines.");
        }

        foreach (var item in _items)
        {
            OrderRules.EnsureValidQuantity(item.Quantity);
            OrderRules.EnsureValidUnitPrice(item.UnitPrice);
            OrderRules.EnsureValidTotal(item.TotalPrice);
        }

        OrderRules.EnsureValidTotal(TotalAmount);

        EnsureValidWalkInPlan();
    }

    public void Confirm(
        Guid actorUserId,
        DateTimeOffset occurredAtUtc)
    {
        EnsurePending();
        EnsureReadyForPersistence();

        Transition(
            OrderStatus.Pending,
            OrderStatus.Confirmed,
            actorUserId,
            occurredAtUtc);
    }

    public void StartPreparing(
        Guid actorUserId,
        DateTimeOffset occurredAtUtc)
    {
        Transition(
            OrderStatus.Confirmed,
            OrderStatus.Preparing,
            actorUserId,
            occurredAtUtc);
    }

    public void MarkReady(
        Guid actorUserId,
        DateTimeOffset occurredAtUtc)
    {
        Transition(
            OrderStatus.Preparing,
            OrderStatus.Ready,
            actorUserId,
            occurredAtUtc);
    }

    public void Complete(
        Guid actorUserId,
        DateTimeOffset occurredAtUtc)
    {
        Transition(
            OrderStatus.Ready,
            OrderStatus.Completed,
            actorUserId,
            occurredAtUtc);

        CompletedAtUtc = occurredAtUtc.ToUniversalTime();
    }

    public void Cancel(
        string reason,
        Guid actorUserId,
        DateTimeOffset occurredAtUtc)
    {
        if (Status != OrderStatus.Pending &&
            Status != OrderStatus.Confirmed)
        {
            throw new DomainException(
                "Only pending or confirmed orders can be cancelled.");
        }

        var normalizedReason = OrderRules.RequiredText(
            reason,
            OrderRules.MaximumCancellationReasonLength,
            "Cancellation reason");

        Transition(
            Status,
            OrderStatus.Cancelled,
            actorUserId,
            occurredAtUtc,
            normalizedReason);

        CancellationReason = normalizedReason;
    }

    private void EnsureValidWalkInPlan()
    {
        if (DiningStartedAtUtc.HasValue !=
            ExpectedDepartureAtUtc.HasValue)
        {
            throw new DomainException(
                "Both walk-in planning timestamps must be provided together.");
        }

        // Null planning fields are allowed for legacy orders.
        if (!DiningStartedAtUtc.HasValue)
        {
            return;
        }

        if (Type != OrderType.DineIn ||
            !DiningTableId.HasValue ||
            ReservationId.HasValue ||
            ExpectedDepartureAtUtc!.Value <= DiningStartedAtUtc.Value)
        {
            throw new DomainException(
                "The walk-in visit plan is invalid.");
        }
    }

    private void Transition(
        OrderStatus expectedStatus,
        OrderStatus nextStatus,
        Guid actorUserId,
        DateTimeOffset occurredAtUtc,
        string? reason = null)
    {
        if (Status != expectedStatus)
        {
            throw new DomainException(
                $"Cannot change order from {Status} to {nextStatus}.");
        }

        EnsureValidTransitionTime(occurredAtUtc);

        var history = new OrderStatusHistory(
            Id,
            Status,
            nextStatus,
            actorUserId,
            occurredAtUtc,
            reason);

        Status = nextStatus;
        _statusHistory.Add(history);

        MarkUpdated();
    }

    private void EnsureValidTransitionTime(
        DateTimeOffset occurredAtUtc)
    {
        if (occurredAtUtc == default)
        {
            throw new DomainException(
                "Status change timestamp is required.");
        }

        var utc = occurredAtUtc.ToUniversalTime();

        var latestKnownTime = _statusHistory.Count == 0
            ? CreatedAtUtc
            : _statusHistory.Max(entry => entry.OccurredAtUtc);

        if (utc < latestKnownTime)
        {
            throw new DomainException(
                "Status change timestamp cannot precede the order history.");
        }
    }

    private OrderItem FindItem(Guid orderItemId)
    {
        if (orderItemId == Guid.Empty)
        {
            throw new DomainException(
                "Order item identifier is required.");
        }

        return _items.SingleOrDefault(
                   item => item.Id == orderItemId)
               ?? throw new DomainException(
                   "Order item was not found.");
    }

    private void EnsurePending()
    {
        if (Status != OrderStatus.Pending)
        {
            throw new DomainException(
                "Only pending orders can be edited.");
        }
    }
}