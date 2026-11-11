using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Restaurant.Application.Common.Models;

namespace Restaurant.Application.Features.Authentication.Me;

public sealed record GetMeQuery : IRequest<MeResponse>;
