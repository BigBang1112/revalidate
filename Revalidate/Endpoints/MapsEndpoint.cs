namespace Revalidate.Endpoints;

internal sealed class MapsEndpoint : IEndpoint
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("maps")
            .WithTags("Maps")
            .WithOpenApi();

        // get map list
        group.MapGet("", GetMapList)
            .WithDescription("Get a list of available maps.");

        // get map info
        group.MapGet("{mapUid}", GetMapInfo)
            .WithDescription("Get information about a map.");
    }

    private Task GetMapList(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private Task GetMapInfo(string mapUid, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
