using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Abstractions.Services;
using Restaurant.Application.Features.Clients.Services;
using Restaurant.Application.Features.DiningTables.Services;
using Restaurant.Application.Features.Orders.Services;
using Restaurant.Application.Features.Payments.Services;
using Restaurant.Application.Features.Reservations.Services;
using Restaurant.Application.Features.Reviews.Services;
using Resturant.Api.Authentication;
using Resturant.Api.ExceptionHandling;
using Resturant.Infrastructure;
using Resturant.Infrastructure.Clients;
using Resturant.Infrastructure.DiningTables;
using Resturant.Infrastructure.Dishes;
using Resturant.Infrastructure.Orders;
using Resturant.Infrastructure.Payments;
using Resturant.Infrastructure.Persistence.Seeding;
using Resturant.Infrastructure.Reservations;
using Resturant.Infrastructure.Reviews;
using Serilog;

namespace Resturant.Api;

public partial class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Initialize logging before application startup.
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure structured logging from appsettings.
            builder.Services.AddSerilog((services, logger) =>
            {
                logger
                    .ReadFrom.Configuration(builder.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty(
                        "Environment",
                        builder.Environment.EnvironmentName);
            });

            // Configure consistent Problem Details responses.
            builder.Services.AddProblemDetails(options =>
            {
                options.CustomizeProblemDetails = context =>
                {
                    context.ProblemDetails.Instance ??=
                        context.HttpContext.Request.Path.Value;

                    context.ProblemDetails.Extensions["traceId"] =
                        context.HttpContext.TraceIdentifier;
                };
            });

            builder.Services.AddExceptionHandler<ApiExceptionHandler>();

            // Configure controllers and model-binding validation errors.
            builder.Services
                .AddControllers()
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.InvalidModelStateResponseFactory = context =>
                    {
                        var problem = new ValidationProblemDetails(
                            context.ModelState)
                        {
                            Type = "about:blank",
                            Title = "Validation failed",
                            Status = StatusCodes.Status400BadRequest,
                            Detail =
                                "One or more request fields are invalid.",
                            Instance =
                                context.HttpContext.Request.Path.Value
                        };

                        problem.Extensions["traceId"] =
                            context.HttpContext.TraceIdentifier;

                        var result = new BadRequestObjectResult(problem);

                        result.ContentTypes.Add(
                            "application/problem+json");

                        return result;
                    };
                });

            // Register MediatR, validators, pipeline behaviors,
            // and existing Application services.
            global::Restaurant.Application.DependencyInjection
                .AddApplication(builder.Services);

            // Register persistence and existing Infrastructure services.
            builder.Services.AddInfrastructure(
                builder.Configuration);

            // Register the system clock unless already configured.
            builder.Services.TryAddSingleton<TimeProvider>(
                TimeProvider.System);

            // Customer profiles and delivery addresses.
            builder.Services.AddScoped<
                ICustomerProfileDao,
                CustomerProfileDao>();

            builder.Services.AddScoped<
                ICustomerProfileService,
                CustomerProfileService>();

            // Dish reads.
            builder.Services.AddScoped<
                IDishReadDao,
                DishReadDao>();

            // Order data access and creation.
            builder.Services.AddScoped<
                IOrderDao,
                OrderDao>();

            builder.Services.AddScoped<
                IOrderCreationService,
                OrderCreationService>();

            // Order history, status changes, and cancellation.
            builder.Services.AddScoped<
                IOrderWorkflowDao,
                OrderWorkflowDao>();

            builder.Services.AddScoped<
                IOrderWorkflowService,
                OrderWorkflowService>();

            // Cash collection and refunds.
            builder.Services.AddScoped<
                ICashPaymentDao,
                CashPaymentDao>();

            builder.Services.AddScoped<
                ICashPaymentService,
                CashPaymentService>();

            // Payment reads with ownership and pagination.
            builder.Services.AddScoped<
                IPaymentReadDao,
                PaymentReadDao>();

            // Review creation, reads, updates, deletion, and moderation.
            builder.Services.AddScoped<
                IReviewDao,
                ReviewDao>();

            builder.Services.AddScoped<
                IReviewService,
                ReviewService>();

            // Dining table management.
            builder.Services.AddScoped<
                IDiningTableDao,
                DiningTableDao>();

            builder.Services.AddScoped<
                IDiningTableService,
                DiningTableService>();

            // Reservations: availability, creation, and reads.
            builder.Services.AddScoped<
                IReservationDao,
                ReservationDao>();

            builder.Services.AddScoped<
                IReservationService,
                ReservationService>();

            // Register authentication, authorization,
            // CORS, and authentication rate limiting.
            builder.Services.AddSessionAuthentication(
                builder.Configuration);

            builder.Services.AddEndpointsApiExplorer();

            // Configure Swagger and JWT Bearer authentication.
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc(
                    "v1",
                    new OpenApiInfo
                    {
                        Title = "Resturant API",
                        Version = "v1",
                        Description =
                            "Restaurant management system API."
                    });

                options.OperationFilter<AuthHeaderOperationFilter>();

                options.AddSecurityDefinition(
                    "Bearer",
                    new OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        Description =
                            "Enter the access token returned by login."
                    });

                options.AddSecurityRequirement(
                    new OpenApiSecurityRequirement
                    {
                        [
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            }
                        ] = Array.Empty<string>()
                    });
            });

            // Build after registering all dependencies.
            var app = builder.Build();

            // Log HTTP requests with their trace identifier.
            app.UseSerilogRequestLogging(options =>
            {
                options.EnrichDiagnosticContext =
                    (diagnosticContext, httpContext) =>
                    {
                        diagnosticContext.Set(
                            "TraceId",
                            httpContext.TraceIdentifier);
                    };
            });

            app.UseExceptionHandler();

            // Provide Problem Details for empty error responses.
            app.UseStatusCodePages(
                async (StatusCodeContext statusContext) =>
                {
                    var httpContext = statusContext.HttpContext;

                    var statusCode =
                        httpContext.Response.StatusCode;

                    var title = statusCode switch
                    {
                        400 => "Bad request",
                        401 => "Unauthorized",
                        403 => "Forbidden",
                        404 => "Not found",
                        405 => "Method not allowed",
                        409 => "Conflict",
                        429 => "Too many requests",
                        _ => "HTTP error"
                    };

                    await ProblemResponses.WriteAsync(
                        httpContext,
                        statusCode,
                        title);
                });

            if (!app.Environment.IsDevelopment())
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            // Expose Swagger only when enabled in Development.
            if (app.Environment.IsDevelopment() &&
                app.Configuration.GetValue<bool>("Swagger:Enabled"))
            {
                app.UseSwagger();

                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint(
                        "/swagger/v1/swagger.json",
                        "Resturant API v1");
                });
            }

            app.UseRouting();

            app.UseCors(
                AuthenticationRegistration.CorsPolicy);

            app.UseRateLimiter();

            app.UseAuthentication();

            app.UseAuthorization();

            app.MapControllers();

            // Seed Development data only when enabled.
            if (app.Environment.IsDevelopment() &&
                app.Configuration.GetValue<bool>("Seed:Enabled"))
            {
                await using var scope =
                    app.Services.CreateAsyncScope();

                var seeder = scope.ServiceProvider
                    .GetRequiredService<DevelopmentSeeder>();

                await seeder.SeedAsync(
                    app.Lifetime.ApplicationStopping);
            }

            Log.Information(
                "Starting {Application} in {Environment}",
                "Resturant.Api",
                app.Environment.EnvironmentName);

            await app.RunAsync();

            return 0;
        }
        catch (Exception exception)
            when (exception is not HostAbortedException)
        {
            Log.Fatal(
                exception,
                "Application terminated unexpectedly");

            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}