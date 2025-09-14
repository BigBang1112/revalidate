using Revalidate.Api;
using Revalidate.Entities;
using System.Collections.Immutable;

namespace Revalidate.Mapping;

public static class ValidationResultEntityMappingExtensions
{
    public static ValidationResult ToDto(this ValidationResultEntity result) => new()
    {
        Id = result.Id,
        Sha256 = Convert.ToHexStringLower(result.Sha256),
        //Crc32 = result.Crc32,
        FileName = result.FileName,
        ReplayId = result.Replay?.Id,
        GhostId = result.Ghost?.Id,
        IsGhostExtracted = result.IsGhostExtracted,
        StartedAt = result.StartedAt,
        CompletedAt = result.CompletedAt,
        GhostUid = result.GhostUid,
        EventsDuration = result.EventsDuration,
        RaceTime = result.RaceTime,
        WalltimeStartedAt = result.WalltimeStartedAt,
        WalltimeEndedAt = result.WalltimeEndedAt,
        ExeVersion = result.ExeVersion,
        ExeChecksum = result.ExeChecksum,
        OsKind = result.OsKind,
        CpuKind = result.CpuKind,
        RaceSettings = result.RaceSettings,
        ValidationSeed = result.ValidationSeed,
        SteeringWheelSensitivity = result.SteeringWheelSensitivity,
        TitleId = result.TitleId,
        TitleChecksum = result.TitleChecksum is null ? null : Convert.ToHexStringLower(result.TitleChecksum),
        Checkpoints = result.Checkpoints.Select(x => x.ToDto()).ToImmutableList(),
    };
}
