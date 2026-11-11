using FluentValidation;
using MediatR;
using Restaurant.Application.Abstractions.Services;
using Restaurant.Application.Common.Models;
using Resturant.Domain.Common;
using Resturant.Domain.Enums;

namespace Restaurant.Application.Features.Orders.Create;

public sealed record CreateOrderCommand(
    Guid RequestKey,
    CreateOrderRequest Request)
    : IRequest<OrderCreationResult>
{
    public override string ToString() =>
        nameof(CreateOrderCommand);
}

public sealed class CreateOrderCommandValidator
    : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(command => command.RequestKey).NotEmpty();

        RuleFor(command => command.Request)
            .NotNull()
            .SetValidator(new CreateOrderRequestValidator());
    }
}

public sealed class CreateOrderRequestValidator
    : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(request => request.Type).IsInEnum();

        RuleFor(request => request.CustomerName)
            .MaximumLength(OrderRules.MaximumCustomerNameLength);

        RuleFor(request => request.CustomerPhone)
            .MaximumLength(OrderRules.MaximumCustomerPhoneLength);

        RuleFor(request => request.DeliveryAddress)
            .MaximumLength(OrderRules.MaximumDeliveryAddressLength);

        RuleFor(request => request.Notes)
            .MaximumLength(OrderRules.MaximumNotesLength);

        RuleFor(request => request.DeliveryAddressId)
            .Must(id => !id.HasValue || id.Value != Guid.Empty)
            .WithMessage(
                "DeliveryAddressId must be a non-empty identifier.");

        RuleFor(request => request.ReservationId)
            .Must(id => !id.HasValue || id.Value != Guid.Empty)
            .WithMessage(
                "ReservationId must be a non-empty identifier.");

        RuleFor(request => request.Items)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .Must(items =>
                items.Length >= 1 &&
                items.Length <= OrderRules.MaximumOrderLines)
            .WithMessage(
                $"An order must contain between 1 and " +
                $"{OrderRules.MaximumOrderLines} lines.");

        When(request => request.Items is not null, () =>
        {
            RuleForEach(request => request.Items)
                .NotNull()
                .SetValidator(new CreateOrderItemRequestValidator());
        });

        When(request => request.Type == OrderType.DineIn, () =>
        {
            RuleFor(request => request.DiningTableId)
                .NotNull()
                .Must(id => id.HasValue && id.Value != Guid.Empty)
                .WithMessage(
                    "Dine-in orders require a valid table.");
        });

        When(request => request.Type != OrderType.DineIn, () =>
        {
            RuleFor(request => request.DiningTableId).Null();

            RuleFor(request => request.ReservationId)
                .Null()
                .WithMessage(
                    "Only dine-in orders can use a reservation.");
        });

        When(request => request.Type != OrderType.Delivery, () =>
        {
            RuleFor(request => request.DeliveryAddressId).Null();

            RuleFor(request => request.DeliveryAddress)
                .Must(string.IsNullOrWhiteSpace)
                .WithMessage(
                    "Only delivery orders can have a delivery address.");
        });

        When(
            request =>
                request.Type == OrderType.Delivery &&
                !request.DeliveryAddressId.HasValue,
            () =>
            {
                RuleFor(request => request.CustomerName).NotEmpty();
                RuleFor(request => request.CustomerPhone).NotEmpty();
                RuleFor(request => request.DeliveryAddress).NotEmpty();
            });

        When(
            request =>
                request.DeliveryAddressId.HasValue ||
                request.ReservationId.HasValue,
            () =>
            {
                RuleFor(request => request.CustomerName)
                    .Must(string.IsNullOrWhiteSpace)
                    .WithMessage(
                        "CustomerName must be omitted when using a saved address or reservation.");

                RuleFor(request => request.CustomerPhone)
                    .Must(string.IsNullOrWhiteSpace)
                    .WithMessage(
                        "CustomerPhone must be omitted when using a saved address or reservation.");
            });

        When(request => request.DeliveryAddressId.HasValue, () =>
        {
            RuleFor(request => request.DeliveryAddress)
                .Must(string.IsNullOrWhiteSpace)
                .WithMessage(
                    "DeliveryAddress must be omitted when using a saved address.");
        });
    }
}

public sealed class CreateOrderItemRequestValidator
    : AbstractValidator<CreateOrderItemRequest>
{
    public CreateOrderItemRequestValidator()
    {
        RuleFor(item => item.MenuItemId).NotEmpty();

        RuleFor(item => item.Quantity)
            .InclusiveBetween(1, OrderRules.MaximumItemQuantity);

        RuleFor(item => item.Notes)
            .MaximumLength(OrderRules.MaximumItemNotesLength);
    }
}

public sealed class CreateOrderCommandHandler(
    IOrderCreationService orderCreationService)
    : IRequestHandler<CreateOrderCommand, OrderCreationResult>
{
    public Task<OrderCreationResult> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        return orderCreationService.CreateAsync(
            request.RequestKey,
            request.Request,
            cancellationToken);
    }
}