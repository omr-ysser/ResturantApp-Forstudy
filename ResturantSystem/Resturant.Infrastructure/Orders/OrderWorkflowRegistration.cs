using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Abstractions.Services;
using Restaurant.Application.Features.Orders.Services;

namespace Resturant.Infrastructure.Orders;

public static class OrderWorkflowRegistration
{
    public static IServiceCollection AddOrderWorkflows(
        this IServiceCollection services)
    {
        services.TryAddScoped<
            IOrderDao,
            OrderDao>();

        services.TryAddScoped<
            IOrderWorkflowDao,
            OrderWorkflowDao>();

        services.TryAddScoped<
            IOrderCreationService,
            OrderCreationService>();

        services.TryAddScoped<
            IOrderWorkflowService,
            OrderWorkflowService>();

        services.TryAddSingleton<TimeProvider>(
            TimeProvider.System);

        return services;
    }
}