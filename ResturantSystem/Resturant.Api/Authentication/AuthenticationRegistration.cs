using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Restaurant.Application.Abstractions.Authentication;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Common.Models;
using Resturant.Api.Authorization;
using Resturant.Api.ExceptionHandling;

namespace Resturant.Api.Authentication;

public static class AuthenticationRegistration
{
    public const string CorsPolicy = "BrowserClient";

    public static IServiceCollection AddSessionAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<RefreshCookie>();
        services.AddScoped<AuthRequestGuard>();

        services.AddOptions<BrowserSecurityOptions>()
            .Bind(configuration.GetSection(
                BrowserSecurityOptions.SectionName))
            .Validate(
                options => options.IsValid(),
                "BrowserSecurity:AllowedOrigins must contain " +
                "explicit HTTPS origins without paths.")
            .ValidateOnStart();

        var browserOptions = configuration
            .GetSection(BrowserSecurityOptions.SectionName)
            .Get<BrowserSecurityOptions>()
            ?? new BrowserSecurityOptions();

        if (!browserOptions.IsValid())
        {
            throw new InvalidOperationException(
                "Invalid browser security configuration.");
        }

        services.AddCors(options =>
        {
            options.AddPolicy(
                CorsPolicy,
                policy =>
                {
                    policy
                        .WithOrigins(
                            browserOptions.AllowedOrigins)
                        .WithMethods(
                            "GET",
                            "POST",
                            "PUT",
                            "PATCH",
                            "DELETE")
                        .WithHeaders(
                            "Content-Type",
                            "Authorization",
                            AuthRequestGuard.HeaderName)
                        .AllowCredentials();
                });
        });

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy =
                new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
        });

        services.AddSingleton<
            IAuthorizationPolicyProvider,
            PermissionPolicyProvider>();

        services.AddScoped<
            IAuthorizationHandler,
            PermissionAuthorizationHandler>();

        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];
        var secret = configuration["Jwt:Secret"];

        if (string.IsNullOrWhiteSpace(issuer) ||
            string.IsNullOrWhiteSpace(audience) ||
            string.IsNullOrWhiteSpace(secret) ||
            Encoding.UTF8.GetByteCount(secret) < 32)
        {
            throw new InvalidOperationException(
                "Invalid JWT configuration.");
        }

        services
            .AddAuthentication(
                JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;

                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = issuer,
                        ValidAudience = audience,

                        IssuerSigningKey =
                            new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(secret)),

                        ValidAlgorithms =
                        [
                            SecurityAlgorithms.HmacSha256
                        ],

                        NameClaimType = "sub",
                        ClockSkew =
                            TimeSpan.FromSeconds(30)
                    };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var userClaim = context.Principal?
                            .FindFirst("sub")?.Value;

                        var sessionClaim = context.Principal?
                            .FindFirst("sid")?.Value;

                        if (!Guid.TryParse(
                                userClaim,
                                out var userId) ||
                            !Guid.TryParse(
                                sessionClaim,
                                out var sessionId))
                        {
                            context.Fail(
                                "Invalid token identifiers.");

                            return;
                        }

                        var services =
                            context.HttpContext.RequestServices;

                        var sessionDao = services
                            .GetRequiredService<
                                IAuthenticationSessionDao>();

                        var timeProvider = services
                            .GetRequiredService<TimeProvider>();

                        var active =
                            await sessionDao.IsSessionActiveAsync(
                                userId,
                                sessionId,
                                timeProvider.GetUtcNow(),
                                context.HttpContext.RequestAborted);

                        if (!active)
                        {
                            context.Fail(
                                "Session is unavailable.");

                            return;
                        }

                        var authenticationDao = services
                            .GetRequiredService<
                                IAuthenticationDao>();

                        var access =
                            await authenticationDao.GetAccessAsync(
                                userId,
                                context.HttpContext.RequestAborted);

                        if (access is null)
                        {
                            context.Fail(
                                "Account is unavailable.");

                            return;
                        }

                        context.HttpContext.Items[
                            typeof(AccessProfile)] = access;
                    },

                    OnChallenge = async context =>
                    {
                        context.HandleResponse();

                        context.Response.Headers[
                            "WWW-Authenticate"] = "Bearer";

                        await ProblemResponses.WriteAsync(
                            context.HttpContext,
                            StatusCodes.Status401Unauthorized,
                            "Unauthorized",
                            "A valid access token and session " +
                            "are required.");
                    },

                    OnForbidden = async context =>
                    {
                        await ProblemResponses.WriteAsync(
                            context.HttpContext,
                            StatusCodes.Status403Forbidden,
                            "Forbidden",
                            "You do not have permission " +
                            "to perform this operation.");
                    }
                };
            });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode =
                StatusCodes.Status429TooManyRequests;

            options.AddPolicy(
                "login",
                context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        GetClientKey(context),
                        _ => CreateLimiter(5)));

            options.AddPolicy(
                "refresh",
                context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        GetClientKey(context),
                        _ => CreateLimiter(30)));

            options.OnRejected =
                async (context, cancellationToken) =>
                {
                    await ProblemResponses.WriteAsync(
                        context.HttpContext,
                        StatusCodes.Status429TooManyRequests,
                        "Too many requests",
                        "Too many authentication requests. " +
                        "Try again later.");
                };
        });

        return services;
    }

    private static string GetClientKey(
        HttpContext context)
    {
        return context.Connection.RemoteIpAddress?
            .ToString() ?? "unknown";
    }

    private static FixedWindowRateLimiterOptions
        CreateLimiter(int permitLimit)
    {
        return new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        };
    }
}
