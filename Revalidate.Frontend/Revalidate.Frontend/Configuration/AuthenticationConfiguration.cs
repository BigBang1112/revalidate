using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

namespace Revalidate.Frontend.Configuration;

public static class AuthenticationConfiguration
{
    public static void AddAuthenticationServices(this IServiceCollection services, IHostEnvironment environment)
    {
        services.AddCascadingAuthenticationState();

        services.AddDataProtection().SetApplicationName("GbxTools");

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/login";
                options.AccessDeniedPath = "/access-denied";
                options.LogoutPath = "/logout";

                options.Cookie.Name = ".GbxTools.Auth.v1";
                if (!environment.IsDevelopment())
                {
                    options.Cookie.Domain = ".gbx.tools"; // ← shared across subdomains
                }
                options.Cookie.Path = "/";
                options.Cookie.SameSite = SameSiteMode.None; // required for OAuth
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.IsEssential = true;
            });
    }
}
