using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Abstractions.Services;

public interface IPasswordChangeService
{
    Task ChangeAsync(
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken);
}
