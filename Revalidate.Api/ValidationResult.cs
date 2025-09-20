using System.Collections.Immutable;
using TmEssentials;

namespace Revalidate.Api;

public sealed class ValidationResult
{
    public required Guid Id { get; init; }
    public string? Sha256 { get; init; }
    //public required int Crc32 { get; init; }
    public string? FileName { get; init; }
    public required ValidationStatus Status { get; set; }
    public required GameVersion GameVersion { get; init; }
    public Guid? ReplayId { get; init; }
    public Guid? GhostId { get; init; }
    public required bool IsGhostExtracted { get; init; }
    public string? GhostUid { get; set; }
    public required TimeInt32 EventsDuration { get; init; }
    public TimeInt32? RaceTime { get; init; }
    public DateTimeOffset? WalltimeStartedAt { get; init; }
    public DateTimeOffset? WalltimeEndedAt { get; init; }
    public string? ExeVersion { get; init; }
    public required uint ExeChecksum { get; init; }
    public required int OsKind { get; init; }
    public required int CpuKind { get; init; }
    public string? RaceSettings { get; init; }
    public int? ValidationSeed { get; init; }
    public required bool SteeringWheelSensitivity { get; init; }
    public string? TitleId { get; init; }
    public string? TitleChecksum { get; init; }
    public string? Login { get; init; }
    public string? MapUid { get; init; }
    public required string ServerVersion { get; set; }
    public required int NbInputs { get; init; }
    public bool? IsValid { get; init; }
    public bool? IsValidExtracted { get; init; }
    public ValidationClientResult? Client { get; init; }
    public required ImmutableList<ValidationDistroResult> Distros { get; init; }
    public required ImmutableList<GhostCheckpoint> Checkpoints { get; init; }
}