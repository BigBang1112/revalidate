namespace Revalidate.Extensions;

internal static partial class EndpointServiceExtensions
{
    public static partial IServiceCollection AddEndpoints(this IServiceCollection services);

    public static IEndpointRouteBuilder UseEndpoints(this IEndpointRouteBuilder app)
    {
        using var scope = app.ServiceProvider.CreateScope();
        var endpoints = scope.ServiceProvider.GetServices<IEndpoint>();

        foreach (var endpoint in endpoints)
        {
            endpoint.RegisterEndpoints(app);
        }

        return app;
    }
}
