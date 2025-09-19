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
        ServerVersion = result.ServerVersion,
        NbInputs = result.NbInputs,
        IsValid = result.IsValid,
        IsValidExtracted = result.IsValidExtracted,
        Distros = result.Distros.Select(distro => new ValidationDistroResult
        {
            Id = distro.Id,
            DistroId = distro.DistroId,
            Status = distro.Status,
            IsValid = distro.IsValid,
            IsValidExtracted = distro.IsValidExtracted,
            StartedAt = distro.StartedAt,
            EndedAt = distro.EndedAt,
            CompletedAt = distro.CompletedAt,
            DeclaredResult = distro.DeclaredNbCheckpoints == null || distro.DeclaredScore == null
                ? null
                : new ValidationRaceResult
                {
                    NbCheckpoints = distro.DeclaredNbCheckpoints.Value,
                    NbRespawns = distro.DeclaredNbRespawns,
                    Time = distro.DeclaredTime,
                    Score = distro.DeclaredScore.Value,
                },
            ValidatedResult = distro.ValidatedNbRespawns == null || distro.ValidatedNbCheckpoints == null || distro.ValidatedScore == null
                ? null
                : new ValidationRaceResult
                {
                    NbCheckpoints = distro.ValidatedNbCheckpoints.Value,
                    NbRespawns = distro.ValidatedNbRespawns,
                    Time = distro.ValidatedTime,
                    Score = distro.ValidatedScore.Value,
                },
            AccountId = distro.AccountId,
            InputsResult = distro.InputsResult,
            Desc = distro.Desc,
        }).ToImmutableList(),
        Checkpoints = result.Checkpoints.Select(x => x.ToDto()).ToImmutableList(),
    };
}
