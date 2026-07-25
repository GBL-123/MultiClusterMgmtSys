using Microsoft.AspNetCore.Mvc;
using MultiClusterMgmtSys.Components.Auth.Services;
using System.Security.Claims;

namespace MultiClusterMgmtSys.Common.Identity;

internal static class IdentityComponentsEndpointRouteBuilderExtensions
{
    // These endpoints are required by the Identity Razor components defined in the /Components/Account/Pages directory of this project.
    public static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var accountGroup = endpoints.MapGroup("/api");

        accountGroup.MapGet("/logout", async (
            HttpContext context,
            [FromServices] AuthService authService,
            [FromQuery] string returnUrl) =>
        {
            var userName = context.User.FindFirstValue(ClaimTypes.Name);
            if (!string.IsNullOrEmpty(userName))
            {
                await authService.LogoutAsync(userName);
            }
            return TypedResults.LocalRedirect($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
        });

        return accountGroup;
    }
}

