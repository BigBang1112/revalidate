using Revalidate.Api;
using Revalidate.Entities;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;
using TmEssentials;

namespace Revalidate.Services;

public sealed class ValidationJobProcessor : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<ValidationJobProcessor> logger;

    private readonly Channel<Guid> channel;

    private static readonly string ArchivesDir = Path.GetFullPath(Path.Combine("data", "archives"));
    private static readonly string VersionsDir = Path.GetFullPath(Path.Combine("data", "versions"));

    public static readonly ImmutableList<string> Distros = ["noble", "plucky", "bookworm-slim", "alpine", "fedora"];

    private static readonly SemaphoreSlim dbSemaphore = new(1, 1);

    public ValidationJobProcessor(IServiceScopeFactory scopeFactory, ILogger<ValidationJobProcessor> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;

        var options = new BoundedChannelOptions(10)
        {
            FullMode = BoundedChannelFullMode.Wait
        };

        channel = Channel.CreateBounded<Guid>(options);
    }

    public async ValueTask EnqueueAsync(Guid validationRequestId, CancellationToken cancellationToken)
    {
        await channel.Writer.WriteAsync(validationRequestId, cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await PullLatestManiaServerManagerImagesAsync(stoppingToken);

        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var validationService = scope.ServiceProvider.GetRequiredService<IValidationService>();

            var results = await validationService.GetAllIncompleteResultsAsync(stoppingToken);

            foreach (var groupedResults in results.GroupBy(x => (x.GameVersion, x.TitleId, x.ServerVersion)))
            {
                try
                {
                    await ValidateAsync(groupedResults.Key.GameVersion, groupedResults.Key.ServerVersion, groupedResults.Key.TitleId, groupedResults, validationService, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error while validating existing incomplete results for {GameVersion} {ServerVersion} {TitleId}", groupedResults.Key.GameVersion, groupedResults.Key.ServerVersion, groupedResults.Key.TitleId);
                    
                    foreach (var result in groupedResults)
                    {
                        result.Status = ValidationStatus.Failed;
                        await validationService.FinishProcessingAsync(result, stoppingToken);
                    }
                }
            }
        }

        await foreach (var requestId in channel.Reader.ReadAllAsync(stoppingToken))
        {
            await using var scope = scopeFactory.CreateAsyncScope();

            var validationService = scope.ServiceProvider.GetRequiredService<IValidationService>();

            var validationRequest = await validationService.GetRequestByIdAsync(requestId, stoppingToken);

            if (validationRequest is null)
            {
                continue;
            }

            foreach (var groupedResults in validationRequest.Results.Where(x => x.Status == ValidationStatus.Pending).GroupBy(x => (x.GameVersion, x.TitleId, x.ServerVersion)))
            {
                if (groupedResults.Key.GameVersion == GameVersion.None)
                {
                    logger.LogWarning("Skipping some validation results because GameVersion is None!");
                    continue;
                }

                try
                {
                    await ValidateAsync(groupedResults.Key.GameVersion, groupedResults.Key.ServerVersion, groupedResults.Key.TitleId, groupedResults, validationService, stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error while validating request {RequestId} results", validationRequest.Id);

                    foreach (var result in groupedResults)
                    {
                        result.Status = ValidationStatus.Failed;
                        await validationService.FinishProcessingAsync(result, stoppingToken);
                    }
                }
            }

            await validationService.FinishRequestAsync(validationRequest, stoppingToken);
        }
    }

    private static async Task PullLatestManiaServerManagerImagesAsync(CancellationToken cancellationToken)
    {
        await Parallel.ForEachAsync(Distros.Add("latest"), cancellationToken, async (distro, token) =>
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = $"pull bigbang1112/mania-server-manager:{distro}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();

            var stdout = await process.StandardOutput.ReadToEndAsync(token);

            await process.WaitForExitAsync(token);
        });
    }

    private async Task ValidateAsync(GameVersion gameVersion, string version, string? titleId, IEnumerable<ValidationResultEntity> results, IValidationService validationService, CancellationToken cancellationToken)
    {
        await validationService.FillMapsFromExternalSourcesAsync(results, cancellationToken);

        var serverType = gameVersion switch
        {
            GameVersion.TM2020 => "TM2020",
            GameVersion.TM2 => "ManiaPlanet",
            _ => throw new InvalidOperationException("Unsupported game version: " + gameVersion)
        };

        var mapsPath = Path.Combine(VersionsDir, $"{serverType}_{version}", "UserData", "Maps");
        var replaysPath = Path.Combine(VersionsDir, $"{serverType}_{version}", "UserData", "Replays");

        // remove all existing files in Replays
        if (Directory.Exists(replaysPath))
        {
            var files = Directory.GetFiles(replaysPath);
            foreach (var file in files)
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to delete replay file {File}", file);
                }
            }
        }

        Directory.CreateDirectory(mapsPath);
        Directory.CreateDirectory(replaysPath);

        var addedMapIds = new HashSet<Guid>();

        foreach (var result in results)
        {
            await validationService.StartProcessingAsync(result, cancellationToken);

            if (result.Replay is not null)
            {
                var replayFilePath = Path.Combine(replaysPath, $"{result.Id}.Replay.Gbx");
                await File.WriteAllBytesAsync(replayFilePath, result.Replay.Data, cancellationToken);
            }

            if (result.Ghost is not null)
            {
                var ghostFilePath = Path.Combine(replaysPath, $"{result.Id}.Ghost.Gbx");
                await File.WriteAllBytesAsync(ghostFilePath, result.Ghost.Data, cancellationToken);
            }

            if (result.Map is not null && addedMapIds.Add(result.Map.Id))
            {
                // MapUid will overwrite same maps so that there arent issues with it
                var mapFilePath = Path.Combine(mapsPath, $"{result.Map.MapUid}.Map.Gbx");
                await File.WriteAllBytesAsync(mapFilePath, result.Map.File.Data, cancellationToken);
            }
        }

        await SetupServerAsync(serverType, version, results, cancellationToken);

        var resultDict = results.ToDictionary(x => x.Id);

        var tasks = new Dictionary<string, Task[]>();
        var processes = new Dictionary<string, Process>();

        foreach (var distro in Distros)
        {
            // change targetted results from Pending to Processing (or something else to Processing)
            foreach (var result in results)
            {
                var distroResult = result.Distros.FirstOrDefault(x => x.DistroId == distro);

                if (distroResult is not null)
                {
                    await validationService.StartDistroProcessingAsync(distroResult, cancellationToken);
                }
            }

            var args = string.Join(' ', [
                "run",
                "--rm",
                "-e", $"MSM_SERVER_TYPE={serverType}",
                "-e", $"MSM_SERVER_VERSION={version}",
                "-e", $"MSM_SERVER_DOWNLOAD_HOST_TM2020={GetHostUrl(results.First().ServerHostType)}",
                "-e", "MSM_VALIDATE_PATH=.",
                "-e", "MSM_ONLY_STDOUT=True",
                "-e", $"MSM_TITLE={titleId}",
                "-v", $"\"{ArchivesDir}:/app/data/archives\"",
                "-v", $"\"{VersionsDir}:/app/data/servers\"",
                $"bigbang1112/mania-server-manager:{distro}",
            ]);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();

            processes[distro] = process;
            tasks[distro] = [
                ProcessStdoutAsync(resultDict, process, distro, validationService, cancellationToken), 
                ProcessStderrAsync(process, cancellationToken),
                process.WaitForExitAsync(cancellationToken)
            ];
        }

        await Task.WhenAll(tasks.SelectMany(x => x.Value));

        foreach (var process in processes.Values)
        {
            process.Dispose();
        }

        foreach (var result in results)
        {
            // determine global isValid from distros
            result.IsValid = result.Distros.All(x => x.IsValid is null)
                ? null
                : result.Distros.Any(x => x.IsValid.GetValueOrDefault());
            result.IsValidExtracted = result.Distros.All(x => x.IsValidExtracted is null)
                ? null
                : result.Distros.Any(x => x.IsValidExtracted.GetValueOrDefault());

            foreach (var distro in result.Distros)
            {
                if (distro.Status == ValidationStatus.Processing)
                {
                    distro.Status = ValidationStatus.Failed;
                }
            }

            await validationService.FinishProcessingAsync(result, cancellationToken);
        }
    }

    private async Task SetupServerAsync(string serverType, string version, IEnumerable<ValidationResultEntity> results, CancellationToken cancellationToken)
    {
        var titles = string.Join(',', results.Select(x => x.TitleId).Distinct());

        var args = string.Join(' ', [
            "run",
            "--rm",
            "-e", $"MSM_SERVER_TYPE={serverType}",
            "-e", $"MSM_SERVER_VERSION={version}",
            "-e", $"MSM_SERVER_DOWNLOAD_HOST_TM2020={GetHostUrl(results.First().ServerHostType)}",
            "-e", $"MSM_PREPARE_TITLES={titles}",
            "-e", "MSM_ONLY_SETUP=True",
            "-v", $"\"{ArchivesDir}:/app/data/archives\"",
            "-v", $"\"{VersionsDir}:/app/data/servers\"",
            "bigbang1112/mania-server-manager",
        ]);

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        process.Start();

        // TODO send to request logs
        string? line;
        while ((line = await process.StandardOutput.ReadLineAsync(cancellationToken)) is not null)
        {
            logger.LogInformation("Validation setup [stdout]: {Line}", line);
        }

        await process.WaitForExitAsync(cancellationToken);
    }

    private static string GetHostUrl(ServerHostType serverHostType)
    {
        return serverHostType switch
        {
            ServerHostType.Ubisoft => "https://nadeo-download.cdn.ubi.com/trackmania",
            ServerHostType.ManiaPlanet => "http://files.v04.maniaplanet.com/server",
            _ => throw new InvalidOperationException("Unsupported server host type: " + serverHostType)
        };
    }

    private async Task ProcessStderrAsync(Process process, CancellationToken cancellationToken)
    {
        string? line;
        while ((line = await process.StandardError.ReadLineAsync(cancellationToken)) is not null)
        {
            logger.LogInformation("Validation [stderr]: {Line}", line);
        }
    }

    private async Task ProcessStdoutAsync(
        Dictionary<Guid, ValidationResultEntity> resultDict, 
        Process process, 
        string distro, 
        IValidationService validationService, 
        CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;

        try
        {
            await foreach (var validatePathResult in JsonSerializer.DeserializeAsyncEnumerable<JsonElement>(process.StandardOutput.BaseStream, cancellationToken: cancellationToken))
            {
                var fileName = validatePathResult.GetProperty("FileName").GetString();

                if (string.IsNullOrEmpty(fileName))
                {
                    logger.LogWarning("Validation result with empty FileName!");
                    continue;
                }

                var resultId = Guid.Parse(GBX.NET.GbxPath.GetFileNameWithoutExtension(fileName));
                var isGhost = fileName.EndsWith(".Ghost.Gbx", StringComparison.OrdinalIgnoreCase);

                if (!resultDict.TryGetValue(resultId, out var result))
                {
                    logger.LogWarning("Validation result {ResultId} not found in dictionary!", resultId);
                    continue;
                }

                logger.LogInformation("Validation result {ResultId} {Distro}: {Result}", result.Id, distro, validatePathResult);

                var distroResult = result.Distros.FirstOrDefault(x => x.DistroId == distro);

                if (distroResult is null)
                {
                    logger.LogWarning("Validation result {ResultId}: Distro result for {Distro} not found.", result.Id, distro);
                    continue;
                }

                // in case existing distroResult is being updated
                distroResult.Desc = null; // disappears when ghost is valid
                distroResult.ValidatedNbCheckpoints = null;
                distroResult.ValidatedNbRespawns = null;
                distroResult.ValidatedTime = null;
                distroResult.ValidatedScore = null;
                distroResult.RawJsonResult = validatePathResult.GetRawText();

                foreach (var property in validatePathResult.EnumerateObject())
                {
                    switch (property.Name)
                    {
                        case "FileName":
                            // handled beforehand
                            break;
                        case "IsValid":
                            if (isGhost && result.IsGhostExtracted)
                                distroResult.IsValidExtracted = property.Value.GetBoolean();
                            else
                                distroResult.IsValid = property.Value.GetBoolean();
                            break;
                        case "NbCheckpoints":
                            distroResult.DeclaredNbCheckpoints = property.Value.GetInt32();
                            break;
                        case "NbRespawns":
                            distroResult.DeclaredNbRespawns = property.Value.GetInt32();
                            break;
                        case "Time":
                            {
                                var time = (int)property.Value.GetUInt32();
                                distroResult.DeclaredTime = time == -1 ? null : TimeInt32.FromMilliseconds(time);
                            }
                            break;
                        case "Score":
                            distroResult.DeclaredScore = property.Value.GetInt32();
                            break;
                        case "DeclaredResult":
                            foreach (var subProperty in property.Value.EnumerateObject())
                            {
                                switch (subProperty.Name)
                                {
                                    case "NbCheckpoints":
                                        distroResult.DeclaredNbCheckpoints = subProperty.Value.GetInt32();
                                        break;
                                    case "NbRespawns":
                                        var nbRespawns = GetInt32OrUInt32(subProperty.Value);
                                        distroResult.DeclaredNbRespawns = nbRespawns;
                                        break;
                                    case "Time":
                                        var time = GetInt32OrUInt32(subProperty.Value);
                                        distroResult.DeclaredTime = time.HasValue ? TimeInt32.FromMilliseconds(time.Value) : null;
                                        break;
                                    case "Score":
                                        distroResult.DeclaredScore = subProperty.Value.GetInt32();
                                        break;
                                }
                            }
                            break;
                        case "ValidatedResult":
                            if (property.Value.ValueKind != JsonValueKind.Null)
                            {
                                foreach (var subProperty in property.Value.EnumerateObject())
                                {
                                    switch (subProperty.Name)
                                    {
                                        case "NbCheckpoints":
                                            distroResult.ValidatedNbCheckpoints = subProperty.Value.GetInt32();
                                            break;
                                        case "NbRespawns":
                                            distroResult.ValidatedNbRespawns = GetInt32OrUInt32(subProperty.Value);
                                            break;
                                        case "Time":
                                            var time = GetInt32OrUInt32(subProperty.Value);
                                            distroResult.ValidatedTime = time.HasValue ? TimeInt32.FromMilliseconds(time.Value) : null;
                                            break;
                                        case "Score":
                                            distroResult.ValidatedScore = subProperty.Value.GetInt32();
                                            break;
                                    }
                                }
                            }
                            break;
                        case "Inputs":
                            distroResult.InputsResult = property.Value.ToString();
                            break;
                        case "Desc":
                            distroResult.Desc = property.Value.GetString();
                            break;
                        case "AccountId":
                            if (Guid.TryParse(property.Value.GetString(), out var guid))
                            {
                                distroResult.AccountId = guid;
                            }
                            break;
                        case "GameBuild":
                            if (result.ExeVersion != property.Value.GetString())
                            {
                                logger.LogWarning("Validation result {ResultId}: GameBuild mismatch! Expected {Expected}, got {Got}", result.Id, result.ExeVersion, property.Value.GetString());
                            }
                            break;
                        case "MapUid":
                            if (result.MapUid != property.Value.GetString())
                            {
                                logger.LogWarning("Validation result {ResultId}: MapUid mismatch! Expected {Expected}, got {Got}", result.Id, result.MapUid, property.Value.GetString());
                            }
                            break;
                        case "Login":
                            if (result.Login != property.Value.GetString())
                            {
                                logger.LogWarning("Validation result {ResultId}: Login mismatch! Expected {Expected}, got {Got}", result.Id, result.Login, property.Value.GetString());
                            }
                            break;
                        default:
                            logger.LogWarning("Validation result {ResultId}: Unknown property {PropertyName}", result.Id, property.Name);
                            break;
                    }
                }

                await dbSemaphore.WaitAsync(cancellationToken);

                try
                {
                    // this gets completed twice sometimes due to replay+extracted ghost being separate validation results coming from the server
                    // maybe could differentiate completion to PartiallyCompleted or something
                    await validationService.FinishDistroProcessingAsync(distroResult, cancellationToken);
                }
                finally
                {
                    dbSemaphore.Release();
                }
            }
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize validation result JSON for distro {Distro}", distro);
        }
    }

    // nando changed NbRespawns and similar from -1 to int.MaxValue, so this has to unify on the issue
    private int? GetInt32OrUInt32(JsonElement element)
    {
        var l = element.GetInt64();
        return l == -1 || l == int.MaxValue ? null : (int)l;
    }
}
