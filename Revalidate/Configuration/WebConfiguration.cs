using ManiaAPI.NadeoAPI;
using ManiaAPI.NadeoAPI.Extensions.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Revalidate.Api;
using Revalidate.Api.Converters.Json;
using Revalidate.Services;
using System.Text.Json.Serialization;

namespace Revalidate.Configuration;

public static class WebConfiguration
{
    private const string UserAgent = "Revalidate/1.0 (Remote Replay Validation; Discord=bigbang1112)";

    public static void AddWebServices(this IServiceCollection services, IConfiguration config, IHostEnvironment environment)
    {
        services.AddNadeoAPI(options =>
        {
            options.Credentials = new NadeoAPICredentials(
                config["NadeoAPI:Login"]!,
                config["NadeoAPI:Password"]!,
                AuthorizationMethod.DedicatedServer);
        }, configureNadeoServices: builder =>
        {
            builder.ConfigureHttpClient(client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            }).AddStandardResilienceHandler();
        }, configureNadeoLiveServices: builder =>
        {
            builder.ConfigureHttpClient(client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            }).AddStandardResilienceHandler();
        }, configureNadeoMeetServices: builder =>
        {
            builder.ConfigureHttpClient(client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            }).AddStandardResilienceHandler();
        });

        services.AddHttpClient<IMapService, MapService>().ConfigureHttpClient(client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        }).AddStandardResilienceHandler();

        services.AddAuthentication();
        services.AddAuthorization();

        services.AddOpenApi();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
        });

        services.AddHealthChecks();

        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, RevalidateJsonSerializerContext.Default);
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.SerializerOptions.Converters.Add(new JsonTimeInt32Converter());
        });

        services.AddSingleton(TimeProvider.System);

        if (environment.IsDevelopment())
        {
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });
        }
        else 
        {
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins("https://revalidate.gbx.tools")
                          .WithMethods("GET", "POST", "DELETE")
                          .AllowAnyHeader();
                });
            });
        }

        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
        });

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });
    }
}
