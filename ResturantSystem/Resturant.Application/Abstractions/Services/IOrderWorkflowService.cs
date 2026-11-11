using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Restaurant.Application.Common.Models;
using Resturant.Domain.Enums;

namespace Restaurant.Application.Abstractions.Services;

public interface IOrderWorkflowService
{
    Task<OrderResponse> ChangeStatusAsync(
        Guid id,
        OrderStatus status,
        string version,
        CancellationToken cancellationToken);

    Task<OrderResponse> CancelAsync(
        Guid id,
        string reason,
        string version,
        CancellationToken cancellationToken);
}
