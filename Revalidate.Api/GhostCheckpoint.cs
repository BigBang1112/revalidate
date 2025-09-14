using TmEssentials;

namespace Revalidate.Api;

public sealed class GhostCheckpoint
{
    public required TimeInt32? Time { get; set; }
    public int? StuntsScore { get; set; }
    public float? Speed { get; set; }
}
