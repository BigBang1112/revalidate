namespace Revalidate.Endpoints;

public static class MapEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.WithTags("Maps");

        group.MapGet("/maps", GetMaps)
            .WithSummary("Available maps")
            .WithDescription("Get a list of available maps.");

        group.MapGet("/maps/{mapUid}", GetMapByUid)
            .WithSummary("Map by UID")
            .WithDescription("Get information about a map.");
    }

    private static Task GetMaps(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private static Task GetMapByUid(string mapUid, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
