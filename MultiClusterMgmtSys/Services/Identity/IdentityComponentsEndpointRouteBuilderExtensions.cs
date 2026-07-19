using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MultiClusterMgmtSys.Models;
using MultiClusterMgmtSys.Requests;
using MultiClusterMgmtSys.Services;

namespace MultiClusterMgmtSys.Services.Identity;

internal static class IdentityComponentsEndpointRouteBuilderExtensions
{
    // These endpoints are required by the Identity Razor components defined in the /Components/Account/Pages directory of this project.
    public static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var accountGroup = endpoints.MapGroup("/api");

        accountGroup.MapPost("/login", async (
            HttpContext context,
            [FromForm] LoginRequest request,
            [FromServices] AccountService accountService,
            [FromServices] IAntiforgery antiforgery) =>
        {
             if (HttpMethods.IsGet(context.Request.Method))
            {
                //Clear the existing external cookie to ensure a clean login process
                await context.SignOutAsync(IdentityConstants.ExternalScheme);
            }
            await antiforgery.ValidateRequestAsync(context);
            var result = await accountService.LoginAsync(request);
            if (result.Succeeded)
            {
                return TypedResults.LocalRedirect(request.ReturnUrl ?? "/");
            }
            return TypedResults.LocalRedirect("/login");
        });

        //accountGroup.MapGet("/api/logout", async (HttpContext ctx, SignInManager<ApplicationUser> signInManager) =>
        //{
        //    await signInManager.SignOutAsync();
        //    return Results.LocalRedirect("/login");
        //});

        //app.MapPost("/api/register", async (HttpContext ctx, AccountService accountService) =>
        //{
        //    var form = ctx.Request.Form;
        //    var username = form["username"].ToString().Trim();
        //    var password = form["password"].ToString();
        //    var confirmPassword = form["confirmPassword"].ToString();
        //    var displayName = form["displayName"].ToString().Trim();
        //    var returnUrl = string.IsNullOrEmpty(form["returnUrl"]) ? "/" : form["returnUrl"].ToString();

        //    if (password != confirmPassword)
        //    {
        //        return Results.Redirect($"/register?error=mismatch&returnUrl={Uri.EscapeDataString(returnUrl)}&username={Uri.EscapeDataString(username)}");
        //    }

        //    var result = await accountService.RegisterAsync(username, password, string.IsNullOrEmpty(displayName) ? null : displayName);
        //    if (!result.Succeeded)
        //    {
        //        var code = result.Errors.FirstOrDefault()?.Code ?? "unknown";
        //        var mapped = code switch
        //        {
        //            "DuplicateUserName" => "duplicate",
        //            "PasswordTooWeak" or "PasswordTooShort" or "PasswordRequiresLetter" or "PasswordRequiresDigit" or "PasswordRequiresNonAlphanumeric" or "PasswordRequiresUpper" or "PasswordRequiresLower" => "weakpwd",
        //            _ => "unknown"
        //        };
        //        return Results.Redirect($"/register?error={mapped}&returnUrl={Uri.EscapeDataString(returnUrl)}&username={Uri.EscapeDataString(username)}");
        //    }

        //    // Auto sign-in after successful registration
        //    var signIn = await accountService.PasswordSignInAsync(username, password, isPersistent: false);
        //    if (!signIn.Succeeded)
        //    {
        //        return Results.LocalRedirect("/login");
        //    }
        //    return Results.LocalRedirect(returnUrl);
        //});

        return accountGroup;
    }
}
