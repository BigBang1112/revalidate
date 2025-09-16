using Revalidate.Services;

namespace Revalidate.Configuration;

public static class DomainConfiguration
{
    public static void AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IValidationService, ValidationService>();
        services.AddScoped<IMapService, MapService>();

        services.AddSingleton<ValidationJobProcessor>();
        services.AddHostedService(sp => sp.GetRequiredService<ValidationJobProcessor>());
    }
}
