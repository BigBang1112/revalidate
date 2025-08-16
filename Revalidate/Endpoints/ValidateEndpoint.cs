using Microsoft.AspNetCore.Http.HttpResults;
using Revalidate.Endpoints.GitHub;
using Revalidate.Models;
using Revalidate.Services;

namespace Revalidate.Endpoints;

public static class ValidateEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/validate", Validate)
            .WithName("ValidateEndpoint")
            .WithTags("Validation");

        group.MapPost("/validate/against", ValidateAgainst)
            .WithName("ValidateAgainstEndpoint")
            .WithTags("Validation");

        //GitHubUserEndpoint.Map(group.MapGroup("/user"));
    }

    private static async Task<Results<Accepted, BadRequest>> Validate(CancellationToken cancellationToken)
    {
        throw new NotImplementedException("This method is not implemented yet.");
    }

    private static async Task<Results<Accepted, BadRequest>> ValidateAgainst(CancellationToken cancellationToken)
    {
        throw new NotImplementedException("This method is not implemented yet.");
    }
}
