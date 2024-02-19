namespace Revalidate.Endpoints;

internal sealed class ValidateEndpoint : IEndpoint
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("validate")
            .DisableAntiforgery()
            .WithTags("Validate")
            .WithFormOptions(valueCountLimit: 10)
            .WithOpenApi();

        // validate multiple replay.gbx
        group.MapPost("", Validate)
            .WithDescription("Validate a Replay.Gbx, using the map file inside the replay.");

        // validate multiple replay.gbx/ghost.gbx against specific map.gbx 
        // validate multiple replay.gbx/ghost.gbx against known map by the server
        group.MapPost("against", ValidateAgainst)
            .WithDescription("Validate a Replay.Gbx against a different map than the one stored inside the replay, or a Ghost.Gbx against a specific Map.Gbx. If a map file is not provided, the replay is validated against the map stored on the server.");
    }

    internal async Task Validate(IFormFileCollection files, CancellationToken cancellationToken)
    {

    }

    internal async Task ValidateAgainst(IFormFileCollection files, CancellationToken cancellationToken)
    {

    }
}
