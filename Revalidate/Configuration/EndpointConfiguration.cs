using Revalidate.Api;
using Revalidate.Endpoints;
using Revalidate.Models;
using System.Collections.Immutable;

namespace Revalidate.Configuration;

public static class EndpointConfiguration
{
    public static void MapEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", GetRevalidateInformation)
            .WithTags("Revalidate")
            .WithSummary("Welcome");

        ValidationEndpoints.Map(app.MapGroup("/validations"));
        ResultEndpoints.Map(app.MapGroup("/results"));
    }

    private static readonly RevalidateInformation Info = new()
    {
        Message = "Welcome to Revalidate!",
        Git = new GitInformation
        {
            Branch = GitInfo.Branch,
            Commit = GitInfo.CommitHash,
            CommitDate = GitInfo.CommitDate,
            Tag = GitInfo.Tag
        },
        Distros = [ // port to database later
            new DistroInformation("noble", "Ubuntu 24.04 LTS", "Noble Numbatt"),
            new DistroInformation("plucky", "Ubuntu 25.04", "Plucky Puffin"),
            new DistroInformation("bookworm-slim", "Debian 12", "Bookworm"),
            new DistroInformation("alpine", "Alpine 3.22", "+ glibc"),
            new DistroInformation("fedora", "Fedora 42", null),
        ]
    };

    private static RevalidateInformation GetRevalidateInformation() => Info;
}
