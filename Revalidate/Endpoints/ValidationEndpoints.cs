using Microsoft.AspNetCore.Http.HttpResults;
using Revalidate.Api;
using Revalidate.Mapping;
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
        group.WithTags("ValidationRequest");

        group.MapPost("/", Validate)
            .WithName(RouteNames.Validate)
            .WithSummary("Validate a replay or ghost")
            .WithDescription("Validates a Replay.Gbx or Ghost.Gbx file. By default, a Replay.Gbx is validated against the map embedded within the replay itself. Optionally, a different Map.Gbx can be provided as the overriden reference. Ghosts are validated against a supplied map, but if no map is provided, the validation falls back to the server's stored map.")
            .DisableAntiforgery();

        group.MapGet("/{id:guid}", GetById)
            .WithName(RouteNames.GetById)
            .WithSummary("Validation request (by ID)")
            .WithDescription("Returns information about a validation request by ID.");

        group.MapGet("/{id:guid}/events", GetEventsById)
            .WithName(RouteNames.GetEventsById)
            .WithSummary("Validation request events (by ID)")
            .WithDescription("Returns events for a validation request by ID.");

        group.MapDelete("/{id:guid}", Delete)
            .WithName(RouteNames.Delete)
            .WithSummary("Validation request")
            .WithDescription("Deletes a validation request by ID. Returns 404 if it does not exist.")
            .RequireAuthorization();
    }

    private static async Task<Results<Accepted<ValidationRequest>, ValidationProblem>> Validate(
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
                { nameof(files), [$"At most 10 files can be provided (a replay/ghost and optionally a map)."] }
            });
        }

        var result = await validationService.ValidateAsync(files, cancellationToken);

        return result.Match<Results<Accepted<ValidationRequest>, ValidationProblem>>(
            validation => TypedResults.Accepted($"/validations/{validation.Id}", validation.ToDto()),
            failed => TypedResults.ValidationProblem(failed.Errors));
    }

    private static async Task<Results<Ok<ValidationRequest>, NotFound>> GetById(
        Guid id,
        IValidationService validationService,
        CancellationToken cancellationToken)
    {
        var request = await validationService.GetRequestDtoByIdAsync(id, cancellationToken);

        return request is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(request);
    }

    private static async Task<Results<ServerSentEventsResult<ValidationRequest>, NotFound>> GetEventsById(
        Guid id,
        IValidationService validationService,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
        //return TypedResults.ServerSentEvents
    }

    private static async Task<Results<NoContent, NotFound>> Delete(
        Guid id,
        IValidationService validationService,
        CancellationToken cancellationToken)
    {
        var removed = await validationService.DeleteRequestAsync(id, cancellationToken);
        return removed
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }
}
