using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Restaurant.Application.Abstractions.Authentication;

namespace Resturant.Api.Authorization;

public sealed record PermissionRequirement(
    string Permission)
    : IAuthorizationRequirement;

[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Method,
    AllowMultiple = true,
    Inherited = true)]
public sealed class RequirePermissionAttribute
    : AuthorizeAttribute
{
    public const string Prefix = "Permission:";

    public RequirePermissionAttribute(string permission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);

        Policy = Prefix + permission;
    }
}

public sealed class PermissionPolicyProvider(
    IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override Task<AuthorizationPolicy?> GetPolicyAsync(
        string policyName)
    {
        if (!policyName.StartsWith(
                RequirePermissionAttribute.Prefix,
                StringComparison.Ordinal))
        {
            return base.GetPolicyAsync(policyName);
        }

        var permission = policyName[
            RequirePermissionAttribute.Prefix.Length..];

        if (string.IsNullOrWhiteSpace(permission))
        {
            return base.GetPolicyAsync(policyName);
        }

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(
                new PermissionRequirement(permission))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}

public sealed class PermissionAuthorizationHandler(
    ICurrentUser currentUser)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        var allowed = currentUser.Access.Permissions.Contains(
            requirement.Permission,
            StringComparer.Ordinal);

        if (allowed)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
