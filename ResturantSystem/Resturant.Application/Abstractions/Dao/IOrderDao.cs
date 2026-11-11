using Restaurant.Application.Common.Models;
using Resturant.Domain.Entities;

namespace Restaurant.Application.Abstractions.Dao;

public interface IOrderDao
{
    Task<PagedResponse<OrderListItemResponse>> ListAsync(
        OrderListFilter filter,
        Guid? customerScopeUserId,
        CancellationToken cancellationToken);

    Task<OrderReceiptLookup?> FindReceiptAsync(
        Guid userId,
        Guid requestKey,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OrderDishReference>> GetDishReferencesAsync(
        Guid[] menuItemIds,
        CancellationToken cancellationToken);

    Task<MenuItem?> LockDishAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<DiningTable?> LockTableAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<bool> HasActiveTableOrderAsync(
        Guid tableId,
        CancellationToken cancellationToken);

    Task<bool> HasSeatedReservationAsync(
        Guid tableId,
        CancellationToken cancellationToken);

    Task<OrderResponse?> GetAsync(
        Guid id,
        Guid? customerScopeUserId,
        CancellationToken cancellationToken);

    void Add(Order order);
}