using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Restaurant.Application.Common.Models;

namespace Restaurant.Application.Features.Orders.Create;

public static class OrderRequestFingerprint
{
    public static string Create(CreateOrderRequest request)
    {
        var items = request.Items
            .Select(item => new
            {
                item.MenuItemId,
                item.Quantity,
                Notes = Normalize(item.Notes)
            })
            .ToArray();

        if (request.ReservationId.HasValue)
        {
            return Hash(new
            {
                SchemaVersion = 3,
                Type = (int)request.Type,
                request.DiningTableId,
                request.ReservationId,
                Notes = Normalize(request.Notes),
                Items = items
            });
        }

        if (request.DeliveryAddressId.HasValue)
        {
            return Hash(new
            {
                SchemaVersion = 2,
                Type = (int)request.Type,
                request.DiningTableId,
                request.DeliveryAddressId,
                Notes = Normalize(request.Notes),
                Items = items
            });
        }

        return Hash(new
        {
            SchemaVersion = 1,
            Type = (int)request.Type,
            request.DiningTableId,
            CustomerName = Normalize(request.CustomerName),
            CustomerPhone = Normalize(request.CustomerPhone),
            DeliveryAddress = Normalize(request.DeliveryAddress),
            Notes = Normalize(request.Notes),
            Items = items
        });
    }

    private static string Hash<T>(T value)
    {
        var json = JsonSerializer.Serialize(value);

        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}