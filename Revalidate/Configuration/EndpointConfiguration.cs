using Revalidate.Endpoints;

namespace Revalidate.Configuration;

public static class EndpointConfiguration
{
    public static void MapEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", async (context) =>
        {
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Welcome to Revalidate!"
            });
        });

        ValidateEndpoint.Map(app.MapGroup("/validate"));
    }
}
