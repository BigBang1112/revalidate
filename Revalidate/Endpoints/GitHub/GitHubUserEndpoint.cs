using Microsoft.AspNetCore.Http.HttpResults;
using Revalidate.Models;
using Revalidate.Services;

namespace Revalidate.Endpoints.GitHub;

public static class GitHubUserEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{username}", GetUser)
            .CacheOutput(x => x.Tag("github:user").Expire(TimeSpan.FromHours(1)));
    }

    private static async Task<Results<Ok<User>, NotFound>> GetUser(string username, IGitHubService githubService, CancellationToken cancellationToken)
    {
        var user = await githubService.GetUserAsync(username, cancellationToken);

        if (user is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(user);
    }
}
