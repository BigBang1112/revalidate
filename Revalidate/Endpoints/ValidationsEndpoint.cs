using Microsoft.AspNetCore.Http.HttpResults;

namespace Revalidate.Endpoints;

public static class ValidationsEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/validations", GetValidations)
            .WithName("ValidationsEndpoint")
            .WithTags("Validation");
    }

    private static async Task<Results<Ok<List<Validation>>, NotFound>> GetValidations(
        [FromServices] IValidationService validationService,
        CancellationToken cancellationToken)
    {
        var validations = await validationService.GetValidationsAsync(cancellationToken);
        if (validations == null || !validations.Any())
        {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(validations);
    }
}
