using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Abstractions.Services;
using Restaurant.Application.Features.Dishes.Services;

namespace Resturant.Infrastructure.Dishes;

public static class DishReadRegistration
{
    public static IServiceCollection AddDishReads(
        this IServiceCollection services)
    {
        services.TryAddScoped<
            IDishReadDao,
            DishReadDao>();

        services.TryAddScoped<
            IDishManagementDao,
            DishManagementDao>();

        services.TryAddScoped<
            IDishService,
            DishService>();

        return services;
    }
}