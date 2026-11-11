using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Resturant.Api.Authentication;

public sealed class AuthHeaderOperationFilter
    : IOperationFilter
{
    public void Apply(
        OpenApiOperation operation,
        OperationFilterContext context)
    {
        var path =
            context.ApiDescription.RelativePath;

        if (path is null ||
            !path.StartsWith(
                "api/auth/",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        operation.Parameters ??=
            new List<OpenApiParameter>();

        operation.Parameters.Add(
            new OpenApiParameter
            {
                Name = AuthRequestGuard.HeaderName,
                In = ParameterLocation.Header,
                Required = true,
                Description =
                    "Required authentication request header.",
                Schema = new OpenApiSchema
                {
                    Type = "string",
                    Default = new OpenApiString("1")
                }
            });
    }
}
