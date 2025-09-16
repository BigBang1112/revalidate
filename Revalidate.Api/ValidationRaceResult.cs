using TmEssentials;

namespace Revalidate.Api;

public sealed class ValidationRaceResult
{
    public required int NbCheckpoints { get; init; }
    public required int? NbRespawns { get; init; }
    public required TimeInt32? Time { get; init; }
    public required int Score { get; init; }
}
