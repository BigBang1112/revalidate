using Microsoft.EntityFrameworkCore;
using Revalidate.Api;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TmEssentials;

namespace Revalidate.Entities;

[Index(nameof(Sha256))]
public sealed class ValidationResultEntity
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    [Column(TypeName = "BINARY(32)")]
    public required byte[] Sha256 { get; set; }

    //public required int Crc32 { get; set; }

    [StringLength(byte.MaxValue)]
    public string? FileName { get; set; }

    public ValidationStatus Status { get; set; }

    public required GameVersion GameVersion { get; set; }

    public required FileEntity? Replay { get; set; }

    public required FileEntity? Ghost { get; set; }

    public required bool IsGhostExtracted { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public required string? GhostUid { get; set; }

    public required TimeInt32 EventsDuration { get; set; }

    public required TimeInt32? RaceTime { get; set; }

    public required DateTimeOffset? WalltimeStartedAt { get; set; }

    public required DateTimeOffset? WalltimeEndedAt { get; set; }

    [StringLength(byte.MaxValue)]
    public required string? ExeVersion { get; set; }

    public required uint ExeChecksum { get; set; }

    public required int OsKind { get; set; }

    public required int CpuKind { get; set; }

    [StringLength(byte.MaxValue)]
    public required string? RaceSettings { get; set; }

    public required int? ValidationSeed { get; set; }

    public required bool SteeringWheelSensitivity { get; set; }

    [StringLength(byte.MaxValue)]
    public required string? TitleId { get; set; }

    [Column(TypeName = "BINARY(32)")]
    public required byte[]? TitleChecksum { get; set; }

    public required int NbInputs { get; set; }

    [StringLength(byte.MaxValue)]
    public required string? Login { get; set; }

    [StringLength(byte.MaxValue)]
    public required string? MapUid { get; set; }

    public MapEntity? Map { get; set; }

    [StringLength(byte.MaxValue)]
    public required string ServerVersion { get; set; }

    public bool? IsValid { get; set; }
    public bool? IsValidExtracted { get; set; }

    public Dictionary<string, string[]> Problems { get; set; } = [];

    public List<GhostCheckpointEntity> Checkpoints { get; set; } = [];
    public List<GhostInputEntity> Inputs { get; set; } = [];
    public List<ValidationRequestEntity> Requests { get; set; } = [];
    public List<ValidationDistroResultEntity> Distros { get; set; } = [];
}