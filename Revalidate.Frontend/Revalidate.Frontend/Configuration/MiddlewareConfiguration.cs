using Microsoft.AspNetCore.Authentication;
using Revalidate.Api;
using Revalidate.Frontend.Components;
using System.Security.Claims;

namespace Revalidate.Frontend.Configuration;

public static class MiddlewareConfiguration
{
    public static void UseMiddleware(this WebApplication app)
    {
        app.UseForwardedHeaders();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }
        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

        app.UseHttpsRedirection();

        if (!app.Environment.IsDevelopment())
        {
            app.UseResponseCompression();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseAntiforgery();

        app.MapGet("login", async (HttpContext context, string returnUrl = "/") =>
        {
            if (app.Environment.IsDevelopment())
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                    new(ClaimTypes.Role, Roles.User),
                };

                var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "GbxTools"));

                await context.SignInAsync(principal, new() { RedirectUri = returnUrl });
            }
            else
            {
                context.Response.Redirect($"https://identity.gbx.tools/connect?returnUrl={Uri.EscapeDataString(returnUrl)}");
            }
        });

        app.MapGet("logout", async (HttpContext context, string returnUrl = "/") =>
        {
            await context.SignOutAsync(new AuthenticationProperties() { RedirectUri = returnUrl });
        });

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);
    }
}
