using Resturant.Domain.Enums;

namespace Restaurant.Application.Common.Models;

public sealed record CreateOrderItemRequest(
    Guid MenuItemId,
    int Quantity,
    string? Notes);

public sealed record CreateOrderRequest(
    OrderType Type,
    Guid? DiningTableId,
    string? CustomerName,
    string? CustomerPhone,
    string? DeliveryAddress,
    string? Notes,
    CreateOrderItemRequest[] Items,
    Guid? DeliveryAddressId = null,
    Guid? ReservationId = null)
{
    public override string ToString() =>
        nameof(CreateOrderRequest);
}

public sealed record OrderItemResponse(
    Guid Id,
    Guid MenuItemId,
    string ItemName,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice,
    string? Notes);

public sealed record OrderResponse(
    Guid Id,
    string OrderNumber,
    string Type,
    string Status,
    Guid? CustomerUserId,
    Guid? DiningTableId,
    string? CustomerName,
    string? CustomerPhone,
    string? DeliveryAddress,
    string? Notes,
    string Currency,
    decimal TotalAmount,
    DateTimeOffset CreatedAtUtc,
    string Version,
    IReadOnlyList<OrderItemResponse> Items,
    Guid? ReservationId = null);

public sealed record OrderCreationResult(
    OrderResponse Order,
    bool Replayed);

public sealed record OrderReceiptLookup(
    Guid OrderId,
    string RequestHash);

public sealed record OrderDishReference(
    Guid Id,
    Guid CategoryId);