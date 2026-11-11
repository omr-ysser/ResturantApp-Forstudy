using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Resturant.Domain.Common;
using Resturant.Domain.Enums;
using Resturant.Domain.Exceptions;

namespace Resturant.Domain.Entities;

public sealed class DiningTable : BaseEntity
{
    private DiningTable()
    {
    }

    public DiningTable(int number, int capacity)
    {
        UpdateDetails(number, capacity);
    }

    public int Number { get; private set; }

    public int Capacity { get; private set; }

    public TableStatus Status { get; private set; }
        = TableStatus.Available;

    public void UpdateDetails(int number, int capacity)
    {
        if (number <= 0)
            throw new DomainException("Table number must be positive.");

        if (capacity <= 0)
            throw new DomainException("Table capacity must be positive.");

        Number = number;
        Capacity = capacity;

        MarkUpdated();
    }

    public void Occupy()
    {
        if (Status != TableStatus.Available)
            throw new DomainException("Table is not available.");

        Status = TableStatus.Occupied;
        MarkUpdated();
    }

    public void Release()
    {
        if (Status != TableStatus.Occupied)
            throw new DomainException("Table is not occupied.");

        Status = TableStatus.Available;
        MarkUpdated();
    }

    public void TakeOutOfService()
    {
        if (Status != TableStatus.Available)
        {
            throw new DomainException(
                "Only an available table can be taken out of service.");
        }

        Status = TableStatus.OutOfService;
        MarkUpdated();
    }

    public void ReturnToService()
    {
        if (Status != TableStatus.OutOfService)
            throw new DomainException("Table is not out of service.");

        Status = TableStatus.Available;
        MarkUpdated();
    }
}
