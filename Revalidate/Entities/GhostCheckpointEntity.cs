using TmEssentials;

namespace Revalidate.Entities;

public sealed class GhostCheckpointEntity
{
    public int Id { get; set; }
    public TimeInt32? Time { get; set; }
    public int? StuntsScore { get; set; }
    public float? Speed { get; set; }
    public required ValidationResultEntity Ghost { get; set; }
}
