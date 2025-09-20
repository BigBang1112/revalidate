using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Net.Http.Headers;
using Revalidate.Api;
using Revalidate.Services;

namespace Revalidate.Endpoints;

public static class ResultEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.WithTags("Validation result");

        group.MapGet("/", GetAll)
            .WithSummary("Validation results")
            .WithDescription("Returns a list of all validation results.");

        group.MapGet("/{id:guid}", GetById)
            .WithSummary("Validation result (by ID)")
            .WithDescription("Returns the validation result by ID.");

        group.MapGet("/{id:guid}/events", GetEventsById)
            .WithSummary("Validation result events (by ID)")
            .WithDescription("Returns events for a validation result by ID.");

        group.MapGet("/{id:guid}/inputs", GetInputsById)
            .WithSummary("Validation ghost inputs (by ID)")
            .WithDescription("Returns inputs for a validation ghost by ID.");

        group.MapGet("/{resultId:guid}/distros/{distroId}/json", GetJsonByDistroId)
            .WithSummary("Validation result JSON (by ID)")
            .WithDescription("Returns raw validation JSON by ID.");

        group.MapDelete("/{id:guid}", Delete)
            .WithSummary("Validation result")
            .WithDescription("Deletes a validation result by ID. Returns 404 if it does not exist.")
            .RequireAuthorization();

        group.MapGet("/{id:guid}/replay/download", DownloadReplayById)
            .WithSummary("Download replay (by validation result ID)")
            .WithDescription("Downloads the replay file associated with a validation result by ID.");

        group.MapGet("/{id:guid}/ghost/download", DownloadGhostById)
            .WithSummary("Download ghost (by validation result ID)")
            .WithDescription("Downloads the ghost file associated with a validation result by ID.");
    }

    private static async Task<Ok<IEnumerable<ValidationResult>>> GetAll(
        IValidationService validationService,
        CancellationToken cancellationToken)
    {
        var results = await validationService.GetAllResultDtosAsync(cancellationToken);
        return TypedResults.Ok(results);
    }

    private static async Task<Results<Ok<ValidationResult>, NotFound>> GetById(
        Guid id,
        IValidationService validationService,
        CancellationToken cancellationToken)
    {
        var request = await validationService.GetResultDtoByIdAsync(id, cancellationToken);

        return request is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(request);
    }

    private static async Task<Results<ServerSentEventsResult<ValidationResult>, NotFound>> GetEventsById(
        Guid id,
        IValidationService validationService,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
        //return TypedResults.ServerSentEvents
    }

    private static async Task<Ok<IEnumerable<GhostInput>>> GetInputsById(
        Guid id,
        IValidationService validationService,
        CancellationToken cancellationToken)
    {
        var inputs = await validationService.GetResultGhostInputDtosByIdAsync(id, cancellationToken);

        return TypedResults.Ok(inputs);
    }

    private static async Task<Results<ContentHttpResult, NotFound>> GetJsonByDistroId(
        Guid resultId,
        string distroId,
        IValidationService validationService,
        CancellationToken cancellationToken)
    {
        var json = await validationService.GetDistroResultJsonByIdAsync(resultId, distroId, cancellationToken);

        return json is null
            ? TypedResults.NotFound()
            : TypedResults.Content(json, "application/json");
    }

    private static async Task<Results<NoContent, NotFound>> Delete(
        Guid id,
        IValidationService validationService,
        CancellationToken cancellationToken)
    {
        var removed = await validationService.DeleteResultAsync(id, cancellationToken);
        return removed
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }

    private static async Task<Results<FileContentHttpResult, NotFound>> DownloadReplayById(
        Guid id,
        IValidationService validationService,
        CancellationToken cancellationToken)
    {
        var downloadData = await validationService.GetResultReplayDownloadAsync(id, cancellationToken);

        return downloadData is null
            ? TypedResults.NotFound()
            : TypedResults.File(downloadData.Data, "application/gbx", $"{id}.Replay.Gbx", lastModified: downloadData.LastModifiedAt, entityTag: new EntityTagHeaderValue(downloadData.Etag));
    }

    private static async Task<Results<FileContentHttpResult, NotFound>> DownloadGhostById(
        Guid id,
        IValidationService validationService,
        CancellationToken cancellationToken)
    {
        var downloadData = await validationService.GetResultGhostDownloadAsync(id, cancellationToken);

        return downloadData is null
            ? TypedResults.NotFound()
            : TypedResults.File(downloadData.Data, "application/gbx", $"{id}.Ghost.Gbx", lastModified: downloadData.LastModifiedAt, entityTag: new EntityTagHeaderValue(downloadData.Etag));
    }
}
