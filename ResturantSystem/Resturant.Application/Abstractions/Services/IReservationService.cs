using Restaurant.Application.Common.Models;
using Resturant.Domain.Enums;

namespace Restaurant.Application.Abstractions.Services;

public interface IReservationService
{
    Task<ReservationCreationResult> CreateAsync(
        Guid requestKey,
        CreateReservationRequest request,
        CancellationToken cancellationToken);

    Task<ReservationResponse> ChangeStateAsync(
        Guid id,
        ReservationStatus targetStatus,
        string version,
        string? reason,
        CancellationToken cancellationToken);
}