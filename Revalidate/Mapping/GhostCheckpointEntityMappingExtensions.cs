using Revalidate.Api;
using Revalidate.Entities;

namespace Revalidate.Mapping;

public static class GhostCheckpointEntityMappingExtensions
{
    public static GhostCheckpoint ToDto(this GhostCheckpointEntity checkpoint) => new()
    {
        Time = checkpoint.Time,
        StuntsScore = checkpoint.StuntsScore,
        Speed = checkpoint.Speed,
    };
}
