using ManiaAPI.NadeoAPI;
using Revalidate.Converters.Json;
using Revalidate.Services;
using System.Text.Json.Serialization;

namespace Revalidate.Configuration;

public static class WebConfiguration
{
    private const string UserAgent = "Revalidate/1.0 (Remote Replay Validation; Discord=bigbang1112)";

    public static void AddWebServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddHttpClient<IMapService, MapService>()
            .ConfigureHttpClient(client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            })
            .AddStandardResilienceHandler();

        services.AddSingleton(new NadeoAPIHandler
        {
            PendingCredentials = new NadeoAPICredentials(
                config["NadeoAPI:Login"]!,
                config["NadeoAPI:Password"]!,
                AuthorizationMethod.DedicatedServer)
        });

        services.AddHttpClient<NadeoServices>().ConfigureHttpClient(client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        }).AddStandardResilienceHandler();

        services.AddHttpClient<NadeoLiveServices>().ConfigureHttpClient(client =>
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
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.SerializerOptions.Converters.Add(new JsonTimeInt32Converter());
        });

        services.AddSingleton(TimeProvider.System);
    }
}
