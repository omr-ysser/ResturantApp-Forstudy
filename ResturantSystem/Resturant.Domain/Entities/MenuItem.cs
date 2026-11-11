using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Resturant.Domain.Common;
using Resturant.Domain.Exceptions;

namespace Resturant.Domain.Entities;

public sealed class MenuItem : BaseEntity
{
    private MenuItem()
    {
    }

    public MenuItem(
        Guid categoryId,
        string name,
        decimal price,
        string? description = null)
    {
        ChangeCategory(categoryId);
        UpdateDetails(name, description);
        ChangePrice(price);
    }

    public Guid CategoryId { get; private set; }

    public Category Category { get; private set; } = null!;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public decimal Price { get; private set; }

    public bool IsAvailable { get; private set; } = true;

    public void UpdateDetails(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Menu item name is required.");

        Name = name.Trim();
        Description = description?.Trim();

        MarkUpdated();
    }

    public void ChangeCategory(Guid categoryId)
    {
        if (categoryId == Guid.Empty)
            throw new DomainException("Category is required.");

        CategoryId = categoryId;
        MarkUpdated();
    }

    public void ChangePrice(decimal price)
    {
        if (price <= 0 || decimal.Round(price, 2) != price)
        {
            throw new DomainException(
                "Price must be positive with at most two decimal places.");
        }

        Price = price;
        MarkUpdated();
    }

    public void SetAvailability(bool isAvailable)
    {
        IsAvailable = isAvailable;
        MarkUpdated();
    }
}
