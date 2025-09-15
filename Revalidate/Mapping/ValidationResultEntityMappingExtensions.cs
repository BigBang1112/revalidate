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
        Status = result.Status,
        GameVersion = result.GameVersion,
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
        Login = result.Login,
        MapUid = result.MapUid,
        IsValid = result.IsValid,
        DeclaredResult = new ValidationRaceResult
        {
            NbCheckpoints = result.DeclaredNbCheckpoints,
            NbRespawns = result.DeclaredNbRespawns,
            Time = result.DeclaredTime,
            Score = result.DeclaredScore,
        },
        ValidatedResult = result.ValidatedNbRespawns is null || result.ValidatedNbCheckpoints is null || result.ValidatedScore is null
            ? null
            : new ValidationRaceResult
            {
                NbCheckpoints = result.ValidatedNbCheckpoints.Value,
                NbRespawns = result.ValidatedNbRespawns.Value,
                Time = result.ValidatedTime,
                Score = result.ValidatedScore.Value,
            },
        AccountId = result.AccountId,
        InputsResult = result.InputsResult,
        NbInputs = result.NbInputs,
        Checkpoints = result.Checkpoints.Select(x => x.ToDto()).ToImmutableList(),
    };
}
