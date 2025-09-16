using Revalidate.Endpoints;
using Revalidate.Models;

namespace Revalidate.Configuration;

public static class EndpointConfiguration
{
    public static void MapEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", GetRevalidateInformation)
            .WithTags("Revalidate");

        ValidationEndpoints.Map(app.MapGroup("/validations"));
        ResultEndpoints.Map(app.MapGroup("/results"));
        Endpoints.MapEndpoints.Map(app.MapGroup("/maps"));
    }

    private static readonly RevalidateInformation Info = new();

    private static RevalidateInformation GetRevalidateInformation() => Info;
}
