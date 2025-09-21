using System.Collections.Immutable;

namespace Revalidate.Api;

public sealed class RevalidateInformation
{
    public required string Message { get; init; }
    public GitInformation? Git { get; init; }
    public required ImmutableList<DistroInformation> Distros { get; init; }
}
