using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resturant.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public DateTimeOffset CreatedAtUtc { get; private set; }
        = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    protected void MarkUpdated()
    {
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
