using Microsoft.AspNetCore.Http.HttpResults;
using Revalidate.Models;
using Revalidate.Services;

namespace Revalidate.Endpoints;

public static class ValidationEndpoints
{
    private static class RouteNames
    {
        public const string GetAll = "Validation_GetAll";
        public const string GetById = "Validation_GetById";
        public const string GetEventsById = "Validation_GetEventsById";
        public const string Validate = "Validation_Validate";
        public const string Delete = "Validation_Delete";
    }

    public static void Map(RouteGroupBuilder group)
    {
        group.WithTags("Validation");

        group.MapPost("/", Validate)
            .WithName(RouteNames.Validate)
            .WithSummary("Validate a Replay.Gbx or Ghost.Gbx, optionally using a map as the reference.")
            .WithDescription("Validates a Replay.Gbx or Ghost.Gbx file. By default, a Replay.Gbx is validated against the map embedded within the replay itself. Optionally, a different Map.Gbx can be provided as the overriden reference. Ghosts are validated against a supplied map, but if no map is provided, the validation falls back to the server's stored map.")
            .DisableAntiforgery();

        group.MapGet("/{id:guid}", GetById)
            .WithName(RouteNames.GetById)
            .WithSummary("Get a validation by ID.")
            .WithDescription("Returns information about a single validation by ID.");

        group.MapGet("/{id:guid}/events", GetEventsById)
            .WithName(RouteNames.GetEventsById)
            .WithSummary("Get events of a validation by ID.")
            .WithDescription("Returns events for a single validation by ID.");

        group.MapGet("/", GetAll)
            .WithName(RouteNames.GetAll)
            .WithSummary("Get a list of complete and in-progress validations.")
            .WithDescription("Returns the list of available validations (may be empty).");

        group.MapDelete("/{id:guid}", Delete)
            .WithName(RouteNames.Delete)
            .WithSummary("Delete a validation result.")
            .WithDescription("Deletes a validation by ID. Returns 404 if it does not exist.");
    }

    private static async Task<Results<Accepted<ValidationResult>, ValidationProblem>> Validate(
        IFormFileCollection files,
        IValidationService validationService,
        CancellationToken cancellationToken)
    {
        if (files.Count == 0)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                { nameof(files), ["At least one file must be provided."] }
            });
        }

        if (files.Count > 10)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                { nameof(files), [$"At most 10 files can be provided (a replay/ghost and optionally a map). Provided files:"] }
            });
        }

        var result = await validationService.ValidateAsync(files, cancellationToken);

        return result.Match<Results<Accepted<ValidationResult>, ValidationProblem>>(
            validation => TypedResults.Accepted($"/validations/{validation.Id}", validation),
            failed => TypedResults.ValidationProblem(failed.Errors));
    }

    private static async Task<Ok<IReadOnlyList<ValidationResult>>> GetAll(
        IValidationService validationService,
        CancellationToken cancellationToken)
    {
        var list = await validationService.GetValidationsAsync(cancellationToken) ?? [];
        return TypedResults.Ok((IReadOnlyList<ValidationResult>)list);
    }

    private static async Task<Results<Ok<ValidationResult>, NotFound>> GetById(
        Guid id,
        IValidationService validationService,
        CancellationToken cancellationToken)
    {
        var entity = await validationService.GetValidationByIdAsync(id, cancellationToken);

        return entity.Match<Results<Ok<ValidationResult>, NotFound>>(
            validation => TypedResults.Ok(validation),
            _ => TypedResults.NotFound());
    }

    private static async Task<Results<NoContent, NotFound>> Delete(
        Guid id,
        IValidationService validationService,
        CancellationToken cancellationToken)
    {
        var removed = await validationService.DeleteValidationAsync(id, cancellationToken);
        return removed
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }

    private static async Task<Results<ServerSentEventsResult<Guid>, NotFound>> GetEventsById(
        Guid id,
        IValidationService validationService,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
        //return TypedResults.ServerSentEvents
    }
}
