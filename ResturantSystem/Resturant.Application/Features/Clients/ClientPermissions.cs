using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Restaurant.Application.Features.Clients;

public static class ClientPermissions
{
    public const string View =
        "Clients.View";

    public const string ViewOwn =
        "Clients.ViewOwn";

    public const string CreateOwn =
        "Clients.CreateOwn";

    public const string EditOwn =
        "Clients.EditOwn";
}
