using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Restaurant.Application.Abstractions.Authentication;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Abstractions.Persistence;
using Restaurant.Application.Common.Security;
using Resturant.Infrastructure.Authentication;
using Resturant.Infrastructure.Categories;
using Resturant.Infrastructure.Dishes;
using Resturant.Infrastructure.Permissions;
using Resturant.Infrastructure.Persistence;
using Resturant.Infrastructure.Persistence.Seeding;

namespace Resturant.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString(
                "DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is required.");
        }

        // Database context.
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        services.AddScoped<IAppDbContext>(provider =>
            provider.GetRequiredService<AppDbContext>());

        // JWT configuration.
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(
                JwtOptions.SectionName))
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(options.Issuer) &&
                    !string.IsNullOrWhiteSpace(options.Audience) &&
                    !string.IsNullOrWhiteSpace(options.Secret) &&
                    Encoding.UTF8.GetByteCount(
                        options.Secret) >= 32 &&
                    options.ExpirationMinutes is >= 1 and <= 120,
                "Invalid JWT configuration.")
            .ValidateOnStart();

        // Authentication session configuration.
        services.AddOptions<SessionOptions>()
            .Bind(configuration.GetSection(
                SessionOptions.SectionName))
            .Validate(
                options =>
                    options.AbsoluteLifetimeDays is >= 1 and <= 90 &&
                    options.RefreshLifetimeDays >= 1 &&
                    options.RefreshLifetimeDays <=
                    options.AbsoluteLifetimeDays,
                "Invalid authentication session configuration.")
            .ValidateOnStart();

        services.AddSingleton<SessionOptions>(provider =>
            provider.GetRequiredService<
                IOptions<SessionOptions>>().Value);

        // Clock.
        services.TryAddSingleton<TimeProvider>(
            TimeProvider.System);

        // Authentication infrastructure.
        services.AddSingleton<
            IPasswordHasher,
            Pbkdf2PasswordHasher>();

        services.AddSingleton<
            IRefreshTokenGenerator,
            RefreshTokenGenerator>();

        services.AddSingleton<
            ITokenService,
            JwtTokenService>();

        // Authentication DAOs.
        services.AddScoped<
            IAuthenticationDao,
            AuthenticationDao>();

        services.AddScoped<
            IAuthenticationSessionDao,
            AuthenticationSessionDao>();

        services.AddScoped<
            IPasswordCredentialDao,
            PasswordCredentialDao>();

        // User DAOs.
        services.AddScoped<
            IUserAccessDao,
            UserAccessDao>();

        services.AddScoped<
            IUserAccountDao,
            UserAccountDao>();

        // Permission DAOs.
        services.AddScoped<
            IPermissionReadDao,
            PermissionReadDao>();

        services.AddScoped<
            IPermissionManagementDao,
            PermissionManagementDao>();

        // Role DAOs.
        services.AddScoped<
            IRolePermissionDao,
            RolePermissionDao>();

        services.AddScoped<
            IRoleManagementDao,
            RoleManagementDao>();

        // Category DAO.
        services.AddScoped<
            ICategoryDao,
            CategoryDao>();

        // Dish DAOs.
        services.AddScoped<
            IDishReadDao,
            DishReadDao>();

        services.AddScoped<
            IDishManagementDao,
            DishManagementDao>();

        // Development seed data.
        services.AddScoped<DevelopmentSeeder>();

        return services;
    }
}