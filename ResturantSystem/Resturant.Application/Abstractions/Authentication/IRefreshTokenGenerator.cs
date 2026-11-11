using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Application.Abstractions.Authentication;

public interface IRefreshTokenGenerator
{
    string Generate();

    string Hash(string token);
}
