using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Restaurant.Application.Abstractions.Services;
using Restaurant.Application.Common.Behaviors;
using Restaurant.Application.Common.Security;
using Restaurant.Application.Features.Authentication.Services;
using Restaurant.Application.Features.Dishes.Services;
using Restaurant.Application.Features.MenuCategories.Services;
using Restaurant.Application.Features.Permissions.Services;
using Restaurant.Application.Features.Roles.Services;
using Restaurant.Application.Features.Users.Services;

namespace Restaurant.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        var assembly =
            typeof(DependencyInjection).Assembly;

        // MediatR handlers and validation pipeline.
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(
                assembly);

            configuration.AddOpenBehavior(
                typeof(ValidationBehavior<,>));
        });

        // FluentValidation validators.
        services.AddValidatorsFromAssembly(assembly);

        // Authentication support.
        services.AddSingleton<DummyPasswordHash>();

        // Authentication services.
        services.AddScoped<
            IAuthenticationService,
            AuthenticationService>();

        services.AddScoped<
            IPasswordChangeService,
            PasswordChangeService>();

        // Permission services.
        services.AddScoped<
            IPermissionManagementService,
            PermissionManagementService>();

        // Role services.
        services.AddScoped<
            IRolePermissionService,
            RolePermissionService>();

        services.AddScoped<
            IRoleManagementService,
            RoleManagementService>();

        // User services.
        services.AddScoped<
            IUserAccessService,
            UserAccessService>();

        services.AddScoped<
            IUserAccountService,
            UserAccountService>();

        // Category service.
        services.AddScoped<
            ICategoryService,
            CategoryService>();

        // Dish service.
        services.AddScoped<
            IDishService,
            DishService>();

        return services;
    }
}