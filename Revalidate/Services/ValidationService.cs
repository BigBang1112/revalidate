using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Inputs;
using Microsoft.EntityFrameworkCore;
using OneOf;
using Revalidate.Api;
using Revalidate.Entities;
using Revalidate.Models;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using TmEssentials;

namespace Revalidate.Services;

public interface IValidationService
{
    Task<OneOf<ValidationRequestEntity, ValidationFailed>> ValidateAsync(IEnumerable<IFormFile> files, CancellationToken cancellationToken);
    Task<ValidationRequestEntity?> GetRequestByIdAsync(Guid id, CancellationToken stoppingToken);
    Task<IEnumerable<ValidationRequest>> GetRequestDtosAsync(CancellationToken cancellationToken);
    Task<ValidationRequest?> GetRequestDtoByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> DeleteRequestAsync(Guid id, CancellationToken cancellationToken);
    Task<ValidationResult?> GetResultDtoByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IEnumerable<ValidationResult>> GetAllResultDtosAsync(CancellationToken cancellationToken);
    Task<bool> DeleteResultAsync(Guid id, CancellationToken cancellationToken);
    Task<DownloadContent?> GetResultReplayDownloadAsync(Guid id, CancellationToken cancellationToken);
    Task<DownloadContent?> GetResultGhostDownloadAsync(Guid id, CancellationToken cancellationToken);
    Task<IEnumerable<ValidationResultEntity>> GetAllIncompleteResultsAsync(CancellationToken stoppingToken);
    Task StartProcessingAsync(ValidationResultEntity result, CancellationToken cancellationToken);
    Task FinishProcessingAsync(ValidationResultEntity result, CancellationToken cancellationToken);
}

public sealed class ValidationService : IValidationService
{
    private readonly ValidationJobProcessor validationJobProcessor;
    private readonly AppDbContext db;
    private readonly ILogger<ValidationService> logger;

    public ValidationService(ValidationJobProcessor validationJobProcessor, AppDbContext db, ILogger<ValidationService> logger)
    {
        this.validationJobProcessor = validationJobProcessor;
        this.db = db;
        this.logger = logger;
    }

    public async Task<OneOf<ValidationRequestEntity, ValidationFailed>> ValidateAsync(IEnumerable<IFormFile> files, CancellationToken cancellationToken)
    {
        // if all setup (not actual run validation) of ghosts or replays fails, it becomes validation error

        var processedHashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var errorBag = new ConcurrentDictionary<string, List<string>>();

        var validationRequest = new ValidationRequestEntity
        {
            CreatedAt = DateTimeOffset.UtcNow
        };

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (file.Length == 0)
            {
                // it is first and only error of a file, so it doesn't need appending
                errorBag.TryAdd(file.FileName, ["File is empty."]);
                continue;
            }

            if (file.Length > 8 * 1024 * 1024)
            {
                // it is first and only error of a file, so it doesn't need appending
                errorBag.TryAdd(file.FileName, ["File exceeds the maximum allowed size of 8MB."]);
                continue;
            }

            await using var stream = file.OpenReadStream();

            var hashStartTimestamp = Stopwatch.GetTimestamp();

            var sha256 = await SHA256.HashDataAsync(stream, cancellationToken);
            var sha256str = Convert.ToHexStringLower(sha256);

            var hashElapsedTime = Stopwatch.GetElapsedTime(hashStartTimestamp);

            logger.LogInformation("Computed SHA-256 hash for file '{FileName}' in {ElapsedMilliseconds}ms: {Hash}",
                file.FileName, hashElapsedTime.TotalMilliseconds, sha256str);

            if (processedHashes.TryGetValue(sha256str, out var duplicateFileName))
            {
                AppendError(errorBag, file.FileName, $"Duplicate file detected: '{duplicateFileName}'. It will be skipped.");
                continue;
            }

            processedHashes[sha256str] = file.FileName;

            stream.Position = 0;

            try
            {
                var gbx = Gbx.ParseHeader(stream);

                if (gbx is Gbx<CGameCtnReplayRecord> or Gbx<CGameCtnGhost>)
                {
                    var existingResult = await db.ValidationResults
                        .Include(x => x.Checkpoints)
                        .FirstOrDefaultAsync(r => r.Sha256.SequenceEqual(sha256), cancellationToken);

                    if (existingResult is not null)
                    {
                        AppendError(errorBag, file.FileName, $"A validation result for this replay already exists (SHA-256: {sha256str}).");
                        validationRequest.Results.Add(existingResult);
                        continue;
                    }
                }

                stream.Position = 0;

                switch (gbx)
                {
                    case Gbx<CGameCtnReplayRecord> replayGbx:
                        replayGbx = await Gbx.ParseAsync<CGameCtnReplayRecord>(stream, cancellationToken: cancellationToken);
                        replayGbx.FilePath = file.FileName;

                        var replayFileEntity = await ToFileEntityAsync(stream, cancellationToken);

                        await foreach (var result in EnumerateReplayResultsAsync(replayGbx, sha256, replayFileEntity, cancellationToken))
                        {
                            validationRequest.Results.Add(result);
                        }
                        break;
                    case Gbx<CGameCtnGhost> ghostGbx:
                        ghostGbx = await Gbx.ParseAsync<CGameCtnGhost>(stream, cancellationToken: cancellationToken);
                        ghostGbx.FilePath = file.FileName;

                        var ghostFileEntity = await ToFileEntityAsync(stream, cancellationToken);

                        var ghostResult = CreateValidationResult(ghostGbx.FilePath, sha256, replayFileEntity: null, ghostFileEntity, ghostGbx.Node, isGhostExtracted: false, errorBag: new ConcurrentDictionary<string, List<string>>());
                        validationRequest.Results.Add(ghostResult);
                        break;
                    case Gbx<CGameCtnChallenge> mapGbx:
                        mapGbx = await Gbx.ParseAsync<CGameCtnChallenge>(stream, cancellationToken: cancellationToken);
                        // pick Maps folder
                        break;
                    default:
                        AppendError(errorBag, file.FileName, $"File is not one of Replay.Gbx, Ghost.Gbx, or Map.Gbx.");
                        continue;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to parse file '{FileName}' as Gbx.", file.FileName);
                AppendError(errorBag, file.FileName, $"File could not be parsed: {ex.Message}");
                continue;
            }
        }

        validationRequest.Warnings = errorBag.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray(), StringComparer.OrdinalIgnoreCase);

        await db.ValidationRequests.AddAsync(validationRequest, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        if (validationRequest.Results.Any(x => x.Status == ValidationStatus.Pending))
        {
            await validationJobProcessor.EnqueueAsync(validationRequest.Id, cancellationToken);
        }

        return validationRequest;
    }

    public async Task<IEnumerable<ValidationRequest>> GetRequestDtosAsync(CancellationToken cancellationToken)
    {
        return [];
    }

    public async Task<ValidationRequestEntity?> GetRequestByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.ValidationRequests
            .Include(x => x.Results)
                .ThenInclude(x => x.Replay)
            .Include(x => x.Results)
                .ThenInclude(x => x.Ghost)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<ValidationRequest?> GetRequestDtoByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.ValidationRequests
            .Select(request => new ValidationRequest
            {
                Id = request.Id,
                CreatedAt = request.CreatedAt,
                CompletedAt = request.CompletedAt,
                Warnings = request.Warnings,
                Results = request.Results.Select(result => new ValidationResult
                {
                    Id = result.Id,
                    Sha256 = Convert.ToHexStringLower(result.Sha256),
                    //Crc32 = result.Crc32,
                    Status = result.Status,
                    GameVersion = result.GameVersion,
                    FileName = result.FileName,
                    ReplayId = result.Replay == null ? null : result.Replay.Id,
                    GhostId = result.Ghost == null ? null : result.Ghost.Id,
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
                    TitleChecksum = result.TitleChecksum == null ? null : Convert.ToHexStringLower(result.TitleChecksum),
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
                    ValidatedResult = result.ValidatedNbRespawns == null || result.ValidatedNbCheckpoints == null || result.ValidatedScore == null
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
                    Checkpoints = result.Checkpoints.OrderBy(x => x.Id).Select(checkpoint => new GhostCheckpoint
                    {
                        Time = checkpoint.Time,
                        StuntsScore = checkpoint.StuntsScore,
                        Speed = checkpoint.Speed
                    }).ToImmutableList(),
                }).ToImmutableList()
            })
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<bool> DeleteRequestAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.ValidationRequests
            .Where(r => r.Id == id)
            .ExecuteDeleteAsync(cancellationToken) > 0;
    }

    public async Task<ValidationResult?> GetResultDtoByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.ValidationResults
            .Where(r => r.Id == id)
            .Select(result => new ValidationResult
            {
                Id = result.Id,
                Sha256 = Convert.ToHexStringLower(result.Sha256),
                //Crc32 = result.Crc32,
                FileName = result.FileName,
                Status = result.Status,
                GameVersion = result.GameVersion,
                ReplayId = result.Replay == null ? null : result.Replay.Id,
                GhostId = result.Ghost == null ? null : result.Ghost.Id,
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
                TitleChecksum = result.TitleChecksum == null ? null : Convert.ToHexStringLower(result.TitleChecksum),
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
                ValidatedResult = result.ValidatedNbRespawns == null || result.ValidatedNbCheckpoints == null || result.ValidatedScore == null
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
                Checkpoints = result.Checkpoints.OrderBy(x => x.Id).Select(checkpoint => new GhostCheckpoint
                {
                    Time = checkpoint.Time,
                    StuntsScore = checkpoint.StuntsScore,
                    Speed = checkpoint.Speed
                }).ToImmutableList(),
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<ValidationResult>> GetAllResultDtosAsync(CancellationToken cancellationToken)
    {
        return [];
    }

    public async Task<bool> DeleteResultAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.ValidationResults
            .Where(r => r.Id == id)
            .ExecuteDeleteAsync(cancellationToken) > 0;
    }

    public async Task<IEnumerable<ValidationResultEntity>> GetAllIncompleteResultsAsync(CancellationToken cancellationToken)
    {
        return await db.ValidationResults
            .Where(r => r.Status == ValidationStatus.Pending || r.Status == ValidationStatus.Processing)
            .Include(x => x.Replay)
            .Include(x => x.Ghost)
            .ToListAsync(cancellationToken);
    }

    public async Task<DownloadContent?> GetResultReplayDownloadAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.ValidationResults
            .Include(x => x.Replay)
            .Where(x => x.Id == id && x.Replay != null)
            .Select(x => new DownloadContent
            {
                Data = x.Replay!.Data,
                LastModifiedAt = x.Replay.LastModifiedAt,
                Etag = x.Replay.Etag
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<DownloadContent?> GetResultGhostDownloadAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.ValidationResults
            .Include(x => x.Ghost)
            .Where(x => x.Id == id && x.Ghost != null)
            .Select(x => new DownloadContent
            {
                Data = x.Ghost!.Data,
                LastModifiedAt = x.Ghost.LastModifiedAt,
                Etag = x.Ghost.Etag
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task StartProcessingAsync(ValidationResultEntity result, CancellationToken cancellationToken)
    {
        result.Status = ValidationStatus.Processing;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task FinishProcessingAsync(ValidationResultEntity result, CancellationToken cancellationToken)
    {
        result.Status = ValidationStatus.Completed;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async IAsyncEnumerable<ValidationResultEntity> EnumerateReplayResultsAsync(Gbx<CGameCtnReplayRecord> replayGbx, byte[] sha256, FileEntity fileEntity, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var replay = replayGbx.Node;

        foreach (var (i, ghost) in replay.GetGhosts().Take(64).Index())
        {
            await using var ms = new MemoryStream();
            ghost.Save(ms);

            var ghostFileEntity = await ToFileEntityAsync(ms, cancellationToken);

            var errorBag = new ConcurrentDictionary<string, List<string>>();

            if (ghost.Validate_ChallengeUid != replay.Challenge?.MapUid)
            {
                errorBag.TryAdd(nameof(ghost.Validate_ChallengeUid), [$"ChallengeUid '{ghost.Validate_ChallengeUid}' does not match the replay's MapUid '{replay.Challenge?.MapUid}'."]);
            }

            yield return CreateValidationResult(replayGbx.FilePath, sha256, fileEntity, ghostFileEntity, ghost, isGhostExtracted: true, errorBag);
        }
    }

    private static ValidationResultEntity CreateValidationResult(
        string? filePath, 
        byte[] sha256, 
        FileEntity? replayFileEntity,
        FileEntity? ghostFileEntity, 
        CGameCtnGhost ghost,
        bool isGhostExtracted,
        ConcurrentDictionary<string, List<string>> errorBag)
    {
        var result = new ValidationResultEntity
        {
            Sha256 = sha256,
            //Crc32
            FileName = filePath,
            GameVersion = ghost.GameVersion switch
            {
                GBX.NET.GameVersion.TM2020 => Api.GameVersion.TM2020,
                GBX.NET.GameVersion.MP4 or GBX.NET.GameVersion.MP4 | GBX.NET.GameVersion.TM2020 => Api.GameVersion.TM2,
                GBX.NET.GameVersion.TMF => Api.GameVersion.TMF,
                _ => Api.GameVersion.None
            },
            Replay = replayFileEntity,
            Ghost = ghostFileEntity,
            IsGhostExtracted = isGhostExtracted,
            GhostUid = ghost.GhostUid.ToString(),
            EventsDuration = ghost.EventsDuration,
            RaceTime = ghost.RaceTime,
            WalltimeStartedAt = ghost.WalltimeStartTimestamp,
            WalltimeEndedAt = ghost.WalltimeEndTimestamp,
            ExeVersion = ghost.Validate_ExeVersion,
            ExeChecksum = ghost.Validate_ExeChecksum,
            OsKind = ghost.Validate_OsKind,
            CpuKind = ghost.Validate_CpuKind,
            RaceSettings = ghost.Validate_RaceSettings,
            ValidationSeed = ghost.Validate_ValidationSeed,
            SteeringWheelSensitivity = ghost.SteeringWheelSensitivity,
            TitleId = ghost.Validate_TitleId,
            TitleChecksum = ghost.Validate_TitleChecksum?.GetBytes(),
            NbInputs = ghost.Inputs?.Count ?? 0,
            Login = ghost.GhostLogin,
            MapUid = ghost.Validate_ChallengeUid
        };

        result.Checkpoints.AddRange(ghost.Checkpoints?.Select(cp => new GhostCheckpointEntity
        {
            Ghost = result,
            Time = cp.Time,
            StuntsScore = cp.StuntsScore,
            Speed = cp.Speed
        }) ?? []);

        result.Inputs.AddRange(ghost.Inputs?.Select(input => new GhostInputEntity
        {
            ValidationResult = result,
            Time = input.Time,
            Name = GetName(input),
            Value = GetValueFromInput(input),
            Pressed = input is IInputState state ? state.Pressed : null,
            X = input is MouseAccu mouseX ? mouseX.X : null,
            Y = input is MouseAccu mouseY ? mouseY.Y : null,
            ValueF = input is SteerOld steerOld ? steerOld.Value : null,
        }) ?? []);

        if (string.IsNullOrWhiteSpace(result.GhostUid))
            AppendError(errorBag, nameof(result.GhostUid), "GhostUid is missing.");

        if (ghost.EventsDuration == default(TimeInt32))
            AppendError(errorBag, nameof(result.EventsDuration), "EventsDuration is 0:00.000.");

        if (result.RaceTime is null)
            AppendError(errorBag, nameof(result.RaceTime), "RaceTime is missing.");
        else if (result.RaceTime == default(TimeInt32))
            AppendError(errorBag, nameof(result.RaceTime), "RaceTime is zero.");
        else if (result.RaceTime < default(TimeInt32))
            AppendError(errorBag, nameof(result.RaceTime), "RaceTime is negative.");

        if (string.IsNullOrWhiteSpace(result.ExeVersion))
            AppendError(errorBag, nameof(result.ExeVersion), "ExeVersion is missing.");

        if (result.ExeChecksum == 0)
            AppendError(errorBag, nameof(result.ExeChecksum), "ExeChecksum is zero.");

        if (string.IsNullOrWhiteSpace(result.RaceSettings))
            AppendError(errorBag, nameof(result.RaceSettings), "RaceSettings is missing.");

        result.Problems = errorBag.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray(), StringComparer.OrdinalIgnoreCase);

        return result;
    }

    private static async Task<FileEntity> ToFileEntityAsync(Stream stream, CancellationToken cancellationToken)
    {
        stream.Position = 0;

        await using var ms = new MemoryStream();

        await stream.CopyToAsync(ms, cancellationToken);

        ms.Position = 0;

        return new FileEntity
        {
            Data = ms.ToArray(),
            LastModifiedAt = DateTimeOffset.UtcNow,
            // Úprava generování ETag, aby odpovídal HTTP standardu (obalení uvozovkami)
            Etag = $"\"{Convert.ToHexStringLower(await MD5.HashDataAsync(ms, cancellationToken))}\""
        };
    }

    private static List<string> AppendError(ConcurrentDictionary<string, List<string>> errorBag, string key, string message)
    {
        return errorBag.AddOrUpdate(key,
            _ => [message],
            (_, list) =>
            {
                list.Add(message);
                return list;
            });
    }

    private static int? GetValueFromInput(IInput input) => input switch
    {
        IInputReal real => real.Value,
        SteerTM2020 steer2020 => steer2020.Value,
        _ => null
    };

    private static string GetName(IInput input) => input switch
    {
        FakeDontInverseAxis => "_FakeDontInverseAxis",
        FakeFinishLine => "_FakeFinishLine",
        FakeIsRaceRunning => "_FakeIsRaceRunning",
        Accelerate => nameof(Accelerate),
        AccelerateReal => nameof(AccelerateReal),
        Brake => nameof(Brake),
        BrakeReal => nameof(BrakeReal),
        Gas => nameof(Gas),
        Horn => nameof(Horn),
        Respawn => nameof(Respawn),
        Steer => nameof(Steer),
        SteerLeft => nameof(SteerLeft),
        SteerRight => nameof(SteerRight),
        _ => throw new ArgumentException($"Unknown input type: {input.GetType()}.", nameof(input))
    };
}