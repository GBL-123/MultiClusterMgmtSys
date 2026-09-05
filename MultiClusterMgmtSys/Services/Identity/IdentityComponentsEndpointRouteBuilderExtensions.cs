using Microsoft.AspNetCore.Mvc;
using MultiClusterMgmtSys.Services;

namespace MultiClusterMgmtSys.Services.Identity;

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
            await authService.LogoutAsync();
            return TypedResults.LocalRedirect($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
        });

        return accountGroup;
    }
}

