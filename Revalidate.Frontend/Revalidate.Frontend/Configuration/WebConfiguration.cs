using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Revalidate.Api;
using System.Net;

namespace Revalidate.Frontend.Configuration;

public static class WebConfiguration
{
    public static void AddWebServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddRazorComponents()
            .AddInteractiveServerComponents().AddHubOptions(options =>
            {
                options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10 MB
            })
            .AddInteractiveWebAssemblyComponents();

        services.AddAuthorizationBuilder()
            .AddDefaultPolicy(Policies.UserPolicy, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(Roles.User);
            })
            .AddPolicy(Policies.AdminPolicy, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(Roles.Admin);
            });

        services.AddHttpClient()
            .ConfigureHttpClientDefaults(httpBuilder =>
            {
                httpBuilder.ConfigureHttpClient(client =>
                {
                    client.BaseAddress = new Uri(config["API:BaseAddress"] ?? throw new InvalidOperationException("API:BaseAddress configuration is missing"));
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("RevalidateFrontend/1.0 (Remote Replay Validation Frontend; Discord=bigbang1112)");
                });
            });

        services.AddScoped(provider =>
        {
            var httpClient = provider.GetRequiredService<HttpClient>();
            return new RevalidateClient(httpClient);
        });

        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

        services.AddHealthChecks();

        // Figures out HTTPS behind proxies
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            foreach (var knownProxy in config.GetSection("KnownProxies").Get<string[]>() ?? [])
            {
                if (IPAddress.TryParse(knownProxy, out var ipAddress))
                {
                    options.KnownProxies.Add(ipAddress);
                    continue;
                }

                foreach (var hostIpAddress in Dns.GetHostAddresses(knownProxy))
                {
                    options.KnownProxies.Add(hostIpAddress);
                }
            }
        });
    }
}
