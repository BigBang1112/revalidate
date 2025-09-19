using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Inputs;
using Microsoft.EntityFrameworkCore;
using OneOf;
using Revalidate.Api;
using Revalidate.Entities;
using Revalidate.Exceptions;
using Revalidate.Models;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using TmEssentials;

namespace Revalidate.Services;

public interface IValidationService
{
    Task<OneOf<ValidationRequestEntity, ValidationFailed>> ValidateAsync(IEnumerable<IFormFile> files, Api.GameVersion? gameVersion, string? mapUid, CancellationToken cancellationToken);
    Task<ValidationRequestEntity?> GetRequestByIdAsync(Guid id, CancellationToken stoppingToken);
    Task<IEnumerable<ValidationRequest>> GetRequestDtosAsync(CancellationToken cancellationToken);
    Task<ValidationRequest?> GetRequestDtoByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> DeleteRequestAsync(Guid id, CancellationToken cancellationToken);
    Task<ValidationResultEntity?> GetResultByIdAsync(Guid resultId, CancellationToken cancellationToken);
    Task<ValidationResult?> GetResultDtoByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IEnumerable<ValidationResult>> GetAllResultDtosAsync(CancellationToken cancellationToken);
    Task<bool> DeleteResultAsync(Guid id, CancellationToken cancellationToken);
    Task<DownloadContent?> GetResultReplayDownloadAsync(Guid id, CancellationToken cancellationToken);
    Task<DownloadContent?> GetResultGhostDownloadAsync(Guid id, CancellationToken cancellationToken);
    Task<IEnumerable<GhostInput>> GetResultGhostInputDtosByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IEnumerable<ValidationResultEntity>> GetAllIncompleteResultsAsync(CancellationToken cancellationToken);
    Task StartProcessingAsync(ValidationResultEntity result, string[] distros, CancellationToken cancellationToken);
    Task StartDistroProcessingAsync(ValidationDistroResultEntity result, CancellationToken cancellationToken);
    Task FinishDistroProcessingAsync(ValidationDistroResultEntity result, CancellationToken cancellationToken);
    Task FinishProcessingAsync(ValidationResultEntity result, CancellationToken cancellationToken);
    Task FinishRequestAsync(ValidationRequestEntity validationRequest, CancellationToken stoppingToken);
    Task FillMapsFromExternalSourcesAsync(IEnumerable<ValidationResultEntity> results, CancellationToken cancellationToken);
}

public sealed partial class ValidationService : IValidationService
{
    private readonly ValidationJobProcessor validationJobProcessor;
    private readonly IMapService mapService;
    private readonly AppDbContext db;
    private readonly HttpClient http;
    private readonly ILogger<ValidationService> logger;

    public ValidationService(ValidationJobProcessor validationJobProcessor, IMapService mapService, AppDbContext db, HttpClient http, ILogger<ValidationService> logger)
    {
        this.validationJobProcessor = validationJobProcessor;
        this.mapService = mapService;
        this.db = db;
        this.http = http;
        this.logger = logger;
    }

    public async Task<OneOf<ValidationRequestEntity, ValidationFailed>> ValidateAsync(IEnumerable<IFormFile> files, Api.GameVersion? gameVersion, string? mapUid, CancellationToken cancellationToken)
    {
        // if all setup (not actual run validation) of ghosts or replays fails, it becomes validation error

        var processedHashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var errorBag = new ConcurrentDictionary<string, List<string>>();

        var validationRequest = new ValidationRequestEntity
        {
            CreatedAt = DateTimeOffset.UtcNow
        };

        var uploadedMaps = new List<UploadedMap>();
        var significantValidationErrorCount = 0;

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (file.Length == 0)
            {
                // it is first and only error of a file, so it doesn't need appending
                errorBag.TryAdd(file.FileName, ["File is empty."]);
                significantValidationErrorCount++;
                continue;
            }

            if (file.Length > 8 * 1024 * 1024)
            {
                // it is first and only error of a file, so it doesn't need appending
                errorBag.TryAdd(file.FileName, ["File exceeds the maximum allowed size of 8MB."]);
                significantValidationErrorCount++;
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
                significantValidationErrorCount++;
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
                        .Include(x => x.Replay)
                        .Include(x => x.Ghost)
                        .Include(x => x.Distros)
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

                        var ghostCount = replayGbx.Node.GetGhosts().Count();
                        if (ghostCount > 1)
                        {
                            AppendError(errorBag, file.FileName, $"Replay contains more than 1 ghost ({ghostCount}). Temporarily, this is not allowed but it will be eventually supported.");
                            continue;
                        }

                        var replayFileEntity = await FileEntity.FromStreamAsync(stream, cancellationToken);

                        await foreach (var result in EnumerateReplayResultsAsync(replayGbx, sha256, replayFileEntity, cancellationToken))
                        {
                            validationRequest.Results.Add(result);
                        }
                        break;
                    case Gbx<CGameCtnGhost> ghostGbx:
                        ghostGbx = await Gbx.ParseAsync<CGameCtnGhost>(stream, cancellationToken: cancellationToken);
                        ghostGbx.FilePath = file.FileName;

                        var ghostFileEntity = await FileEntity.FromStreamAsync(stream, cancellationToken);

                        var ghostResult = CreateValidationResult(ghostGbx.FilePath, sha256, replayFileEntity: null, ghostFileEntity, ghostGbx.Node, isGhostExtracted: false, url: null, errorBag: new ConcurrentDictionary<string, List<string>>());
                        validationRequest.Results.Add(ghostResult);
                        break;
                    case Gbx<CGameCtnChallenge> mapGbx:
                        mapGbx = await Gbx.ParseAsync<CGameCtnChallenge>(stream, cancellationToken: cancellationToken);
                        mapGbx.FilePath = file.FileName;

                        uploadedMaps.Add(new UploadedMap(mapGbx, sha256, await FileEntity.FromStreamAsync(stream, cancellationToken)));
                        break;
                    default:
                        AppendError(errorBag, file.FileName, "File is not one of Replay.Gbx, Ghost.Gbx, or Map.Gbx.");
                        significantValidationErrorCount++;
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
                significantValidationErrorCount++;
                continue;
            }
        }

        foreach (var uploadedMap in uploadedMaps)
        {
            var hasGhost = validationRequest.Results.Any(x => x.MapUid == uploadedMap.MapGbx.Node.MapUid);

            if (!hasGhost)
            {
                AppendError(errorBag, uploadedMap.MapGbx.FilePath ?? Convert.ToHexStringLower(uploadedMap.Sha256), "Map is not associated with any replay or ghost in the request.");
                significantValidationErrorCount++;
                continue;
            }

            MapEntity mapEntity;

            try
            { 
                mapEntity = await mapService.GetOrCreateMapAsync(uploadedMap, cancellationToken);
            }
            catch (MapUidException ex)
            {
                logger.LogWarning(ex, "Failed to process uploaded map '{FileName}'.", uploadedMap.MapGbx.FilePath);
                AppendError(errorBag, uploadedMap.MapGbx.FilePath ?? Convert.ToHexStringLower(uploadedMap.Sha256), ex.Message);
                significantValidationErrorCount++;
                continue;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process uploaded map '{FileName}'.", uploadedMap.MapGbx.FilePath);
                AppendError(errorBag, uploadedMap.MapGbx.FilePath ?? Convert.ToHexStringLower(uploadedMap.Sha256), $"Map could not be processed: {ex.Message}");
                significantValidationErrorCount++;
                continue;
            }

            foreach (var result in validationRequest.Results.Where(x => x.MapUid == uploadedMap.MapGbx.Node.MapUid))
            {
                result.Map = mapEntity;
            }
        }

        // when external validations are done
        if (gameVersion.HasValue && !string.IsNullOrWhiteSpace(mapUid))
        {
            MapEntity? mapEntity;

            try
            {
                mapEntity = await mapService.GetOrCreateMapAsync(gameVersion.Value, mapUid, cancellationToken);
            }
            catch (MapUidException ex)
            {
                AppendError(errorBag, nameof(mapUid), ex.Message);
                return new ValidationFailed(errorBag.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray(), StringComparer.OrdinalIgnoreCase));
            }

            if (mapEntity is null)
            {
                AppendError(errorBag, "Map", $"Map with MapUid '{mapUid}' for game version '{gameVersion}' could not be found or downloaded.");
                return new ValidationFailed(errorBag.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray(), StringComparer.OrdinalIgnoreCase));
            }

            var records = await mapService.GetTopRecordsAsync(mapEntity, cancellationToken);

            foreach (var record in records)
            {
                var fileName = Path.GetFileName(record.FileName);

                logger.LogInformation("Downloading ghost rank: {Rank}", record.Rank);

                using var response = await http.GetAsync(record.Url, cancellationToken);
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

                var ghostFileEntity = await FileEntity.FromStreamAsync(stream, cancellationToken);

                stream.Position = 0;
                var sha256 = await SHA256.HashDataAsync(stream, cancellationToken);

                var result = await db.ValidationResults
                    .Include(x => x.Replay)
                    .Include(x => x.Ghost)
                    .Include(x => x.Distros)
                    .Include(x => x.Checkpoints)
                    .FirstOrDefaultAsync(r => r.Sha256.SequenceEqual(sha256), cancellationToken);

                // have option to either avoid existing validations or update them (revalidate them haha get it)
                /*if (result is not null)
                {
                    var sha256str = Convert.ToHexStringLower(sha256);
                    AppendError(errorBag, fileName, $"A validation result for this replay already exists (SHA-256: {sha256str}).");
                    validationRequest.Results.Add(result);
                    continue;
                }*/

                stream.Position = 0;
                var ghost = await Gbx.ParseNodeAsync<CGameCtnGhost>(stream, cancellationToken: cancellationToken);

                if (result is null)
                {
                    result = CreateValidationResult(fileName, sha256, null, ghostFileEntity, ghost, isGhostExtracted: false, record.Url, errorBag);
                }
                else
                {
                    result.ServerVersion = GetServerVersion(MapGameVersion(ghost.GameVersion), ghost, errorBag, out var hostType);
                    result.ServerHostType = hostType;
                    result.Status = ValidationStatus.Pending;
                }

                result.Map = mapEntity;
                validationRequest.Results.Add(result);
            }
        }

        foreach (var result in validationRequest.Results.Where(x => x.Ghost is not null && x.Map is null))
        {
            if (result.MapUid is null)
            {
                AppendError(errorBag, result.FileName ?? Convert.ToHexStringLower(result.Sha256), "Ghost does not have a MapUid that would allow downloading the map externally.");
                continue;
            }

            result.Map = await mapService.GetMapAsync(result.GameVersion, result.MapUid, cancellationToken);
        }

        if (significantValidationErrorCount > validationRequest.Results.Count)
        {
            logger.LogError("Validation request has {ErrorCount} significant errors exceeding the number of results ({ResultCount}), failing the validate the request, so it proceeds.",
                significantValidationErrorCount, validationRequest.Results.Count);
        }
        else if (significantValidationErrorCount > 0 && significantValidationErrorCount == validationRequest.Results.Count)
        {
            return new ValidationFailed(errorBag.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray(), StringComparer.OrdinalIgnoreCase));
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
            .Include(x => x.Results)
                .ThenInclude(x => x.Distros)
            .Include(x => x.Results)
                .ThenInclude(x => x.Map)
                    .ThenInclude(x => x!.File)
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

    public async Task<ValidationResultEntity?> GetResultByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.ValidationResults
            .Include(x => x.Replay)
            .Include(x => x.Ghost)
            .Include(x => x.Distros)
            .Include(x => x.Checkpoints)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
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
            .Include(x => x.Distros)
            .Include(x => x.Map)
                .ThenInclude(x => x!.File)
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

    public async Task<IEnumerable<GhostInput>> GetResultGhostInputDtosByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await db.GhostInputs
            .Where(input => input.ValidationResultId == id)
            .OrderBy(input => input.Id)
            .Select(input => new GhostInput
            {
                Time = input.Time,
                Name = input.Name,
                Value = input.Value,
                Pressed = input.Pressed,
                X = input.X,
                Y = input.Y,
                ValueF = input.ValueF
            })
            .ToListAsync(cancellationToken);
    }

    public async Task StartProcessingAsync(ValidationResultEntity result, string[] distros, CancellationToken cancellationToken)
    {
        result.Status = ValidationStatus.Processing;

        foreach (var distro in distros.Where(d => !result.Distros.Any(dd => dd.DistroId == d)))
        {
            var distroResult = new ValidationDistroResultEntity
            {
                Result = result,
                DistroId = distro,
                Status = ValidationStatus.Pending
            };

            await db.ValidationDistroResults.AddAsync(distroResult, cancellationToken);
            result.Distros.Add(distroResult);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task StartDistroProcessingAsync(ValidationDistroResultEntity result, CancellationToken cancellationToken)
    {
        result.Status = ValidationStatus.Processing;
        result.StartedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task FinishDistroProcessingAsync(ValidationDistroResultEntity result, CancellationToken cancellationToken)
    {
        result.Status = ValidationStatus.Completed;

        // because FinishDistro can be called twice due to replay+ghost extract then its preferable to not override the time
        result.CompletedAt ??= DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task FinishProcessingAsync(ValidationResultEntity result, CancellationToken cancellationToken)
    {
        result.Status = result.IsValid.HasValue || result.IsValidExtracted.HasValue
            ? ValidationStatus.Completed
            : ValidationStatus.Failed;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task FinishRequestAsync(ValidationRequestEntity validationRequest, CancellationToken cancellationToken)
    {
        validationRequest.CompletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task FillMapsFromExternalSourcesAsync(IEnumerable<ValidationResultEntity> results, CancellationToken cancellationToken)
    {
        foreach (var result in results.Where(x => x.Map is null))
        {
            if (result.MapUid is null)
            {
                logger.LogWarning("Cannot fill map for result {ResultId} because it has no MapUid.", result.Id);
                continue;
            }

            var map = await mapService.GetOrCreateMapAsync(result.GameVersion, result.MapUid, cancellationToken);

            if (map is not null)
            {
                result.Map = map;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async IAsyncEnumerable<ValidationResultEntity> EnumerateReplayResultsAsync(Gbx<CGameCtnReplayRecord> replayGbx, byte[] sha256, FileEntity fileEntity, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var replay = replayGbx.Node;

        foreach (var (i, ghost) in replay.GetGhosts().Take(64).Index())
        {
            await using var ms = new MemoryStream();
            ghost.Save(ms);

            var ghostFileEntity = await FileEntity.FromStreamAsync(ms, cancellationToken);

            var errorBag = new ConcurrentDictionary<string, List<string>>();

            if (ghost.Validate_ChallengeUid != replay.Challenge?.MapUid)
            {
                errorBag.TryAdd(nameof(ghost.Validate_ChallengeUid), [$"ChallengeUid '{ghost.Validate_ChallengeUid}' does not match the replay's MapUid '{replay.Challenge?.MapUid}'."]);
            }

            yield return CreateValidationResult(replayGbx.FilePath, sha256, fileEntity, ghostFileEntity, ghost, isGhostExtracted: true, url: null, errorBag);
        }
    }

    private static ValidationResultEntity CreateValidationResult(
        string? fileName, 
        byte[] sha256, 
        FileEntity? replayFileEntity,
        FileEntity? ghostFileEntity, 
        CGameCtnGhost ghost,
        bool isGhostExtracted,
        string? url,
        ConcurrentDictionary<string, List<string>> errorBag)
    {
        var gameVersion = MapGameVersion(ghost.GameVersion);

        var inputs = ghost.Inputs ?? ghost.PlayerInputs?.FirstOrDefault()?.Inputs ?? [];

        var serverVersion = GetServerVersion(gameVersion, ghost, errorBag, out var hostType);

        var result = new ValidationResultEntity
        {
            Sha256 = sha256,
            //Crc32
            FileName = fileName,
            GameVersion = gameVersion,
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
            NbInputs = inputs.Count,
            Login = ghost.GhostLogin,
            MapUid = ghost.Validate_ChallengeUid,
            ServerVersion = serverVersion,
            ServerHostType = hostType,
            Url = url,
        };

        result.Checkpoints.AddRange(ghost.Checkpoints?.Select(cp => new GhostCheckpointEntity
        {
            Ghost = result,
            Time = cp.Time,
            StuntsScore = cp.StuntsScore,
            Speed = cp.Speed
        }) ?? []);

        result.Inputs.AddRange(inputs.Select(input => new GhostInputEntity
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

        var fileIdent = fileName ?? Convert.ToHexStringLower(sha256);

        if (string.IsNullOrWhiteSpace(result.GhostUid))
            AppendError(errorBag, fileIdent, "GhostUid is missing.");

        if (gameVersion != Api.GameVersion.TM2020 && ghost.EventsDuration == default(TimeInt32))
            AppendError(errorBag, fileIdent, "EventsDuration is 0:00.000.");

        if (result.RaceTime is null)
            AppendError(errorBag, fileIdent, "RaceTime is missing.");
        else if (result.RaceTime == default(TimeInt32))
            AppendError(errorBag, fileIdent, "RaceTime is zero.");
        else if (result.RaceTime < default(TimeInt32))
            AppendError(errorBag, fileIdent, "RaceTime is negative.");

        if (string.IsNullOrWhiteSpace(result.ExeVersion))
            AppendError(errorBag, fileIdent, "ExeVersion is missing.");

        if (result.ExeChecksum == 0)
            AppendError(errorBag, fileIdent, "ExeChecksum is zero.");

        // looks like Nadeo kept it always empty and openplanet took advantage of it
        if (gameVersion != Api.GameVersion.TM2020 && string.IsNullOrWhiteSpace(result.RaceSettings))
            AppendError(errorBag, fileIdent, "RaceSettings is missing.");

        result.Problems = errorBag.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray(), StringComparer.OrdinalIgnoreCase);

        return result;
    }

    private static Api.GameVersion MapGameVersion(GBX.NET.GameVersion gameVersion)
    {
        return gameVersion switch
        {
            GBX.NET.GameVersion.TM2020 => Api.GameVersion.TM2020,
            GBX.NET.GameVersion.MP4 or GBX.NET.GameVersion.MP4 | GBX.NET.GameVersion.TM2020 => Api.GameVersion.TM2,
            GBX.NET.GameVersion.TMF => Api.GameVersion.TMF,
            _ => Api.GameVersion.None
        };
    }

    private static string GetServerVersion(Api.GameVersion gameVersion, CGameCtnGhost ghost, ConcurrentDictionary<string, List<string>> errorBag, out ServerHostType hostType)
    {
        hostType = ServerHostType.Ubisoft;

        if (gameVersion == Api.GameVersion.TM2 || string.IsNullOrWhiteSpace(ghost.Validate_ExeVersion))
        {
            return "Latest";
        }

        var matchExeVersion = ExeVersionRegex().Match(ghost.Validate_ExeVersion);

        if (!DateTimeOffset.TryParseExact(
            matchExeVersion.Groups[2].Value, 
            "yyyy-MM-dd_HH_mm", 
            CultureInfo.InvariantCulture, 
            DateTimeStyles.AssumeUniversal, 
            out var exeDate))
        {
            AppendError(errorBag, nameof(ghost.Validate_ExeVersion), $"Could not parse date from ExeVersion '{ghost.Validate_ExeVersion}'. Using: Latest");
            return "Latest";
        }

        if (exeDate < new DateTimeOffset(2021, 6, 9, 0, 0, 0, TimeSpan.Zero))
        {
            hostType = ServerHostType.ManiaPlanet;
            return "2021-05-31";
        }

        if (exeDate < new DateTimeOffset(2021, 9, 30, 0, 0, 0, TimeSpan.Zero))
        {
            hostType = ServerHostType.ManiaPlanet;
            return "2021-07-07";
        }

        if (exeDate < new DateTimeOffset(2021, 12, 14, 0, 0, 0, TimeSpan.Zero))
        {
            hostType = ServerHostType.ManiaPlanet;
            return "2021-09-29";
        }

        if (exeDate < new DateTimeOffset(2022, 9, 7, 0, 0, 0, TimeSpan.Zero))
        {
            return "2022-06-21";
        }

        if (exeDate < new DateTimeOffset(2022, 9, 9, 0, 0, 0, TimeSpan.Zero))
        {
            return "2022-09-06b";
        }

        if (exeDate < new DateTimeOffset(2022, 9, 29, 0, 0, 0, TimeSpan.Zero))
        {
            return "2022-09-08b";
        }

        return "Latest";
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

    [GeneratedRegex(@"(Trackmania|ManiaPlanet) date=([0-9-_]+) (git|Svn)=([0-9a-f-]+) GameVersion=([0-9.]+)")]
    private static partial Regex ExeVersionRegex();
}