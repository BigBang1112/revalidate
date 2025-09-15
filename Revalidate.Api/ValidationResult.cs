using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using TmEssentials;

namespace Revalidate.Api;

public sealed class ValidationResult
{
    public required Guid Id { get; init; }
    public required string? Sha256 { get; init; }
    //public required int Crc32 { get; init; }
    public required string? FileName { get; init; }
    public required ValidationStatus Status { get; set; }
    public required GameVersion GameVersion { get; init; }
    public required Guid? ReplayId { get; init; }
    public required Guid? GhostId { get; init; }
    public required bool IsGhostExtracted { get; init; }
    public required DateTimeOffset? StartedAt { get; init; }
    public required DateTimeOffset? CompletedAt { get; init; }
    public required string? GhostUid { get; set; }
    public required TimeInt32 EventsDuration { get; init; }
    public required TimeInt32? RaceTime { get; init; }
    public required DateTimeOffset? WalltimeStartedAt { get; init; }
    public required DateTimeOffset? WalltimeEndedAt { get; init; }
    public required string? ExeVersion { get; init; }
    public required uint ExeChecksum { get; init; }
    public required int OsKind { get; init; }
    public required int CpuKind { get; init; }
    public required string? RaceSettings { get; init; }
    public required int? ValidationSeed { get; init; }
    public required bool SteeringWheelSensitivity { get; init; }
    public required string? TitleId { get; init; }
    public required string? TitleChecksum { get; init; }
    public required string? Login { get; init; }
    public required string? MapUid { get; init; }
    public required bool? IsValid { get; init; }
    public required ValidationRaceResult DeclaredResult { get; init; }
    public required ValidationRaceResult? ValidatedResult { get; init; }
    public required Guid? AccountId { get; init; }
    public required string? InputsResult { get; init; }
    public required int NbInputs { get; init; }
    public required ImmutableList<GhostCheckpoint> Checkpoints { get; init; }
}