using CliWrap;
using Revalidate.Api;
using Revalidate.Entities;
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

    private readonly string archivesTempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly string serversTempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

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
        await Cli.Wrap("docker")
            .WithArguments(["pull", "bigbang1112/mania-server-manager"])
            .ExecuteAsync(stoppingToken);

        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var validationService = scope.ServiceProvider.GetRequiredService<IValidationService>();

            var results = await validationService.GetAllIncompleteResultsAsync(stoppingToken);

            foreach (var groupedResults in results.GroupBy(x => (x.GameVersion, x.TitleId)))
            {
                await ValidateAsync(groupedResults.Key.GameVersion, groupedResults.Key.TitleId, groupedResults, scope.ServiceProvider, stoppingToken);
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

            foreach (var groupedResults in validationRequest.Results.Where(x => x.Status == ValidationStatus.Pending).GroupBy(x => (x.GameVersion, x.TitleId)))
            {
                if (groupedResults.Key.GameVersion == GameVersion.None)
                {
                    logger.LogWarning("Skipping some validation results because GameVersion is None!");
                    return;
                }

                await ValidateAsync(groupedResults.Key.GameVersion, groupedResults.Key.TitleId, groupedResults, scope.ServiceProvider, stoppingToken);
            }
        }
    }

    private async Task ValidateAsync(GameVersion gameVersion, string? titleId, IEnumerable<ValidationResultEntity> results, IServiceProvider provider, CancellationToken cancellationToken)
    {
        var serverType = gameVersion switch
        {
            GameVersion.TM2020 => "TM2020",
            GameVersion.TM2 => "ManiaPlanet",
            _ => throw new InvalidOperationException("Unsupported game version: " + gameVersion)
        };

        var replaysPath = Path.Combine(serversTempDir, $"{serverType}_Latest", "UserData", "Replays");
        Directory.CreateDirectory(replaysPath);

        var validationService = provider.GetRequiredService<IValidationService>();

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
        }

        var titles = string.Join(',', results.Select(x => x.TitleId).Distinct());
        var resultDict = results.ToDictionary(x => x.Id);

        var args = string.Join(' ', [
            "run",
            "--rm",
            "-e", $"MSM_SERVER_TYPE={serverType}",
            "-e", "MSM_VALIDATE_PATH=.",
            "-e", "MSM_ONLY_STDOUT=True",
            "-e", $"MSM_TITLE={titleId}",
            "-e", $"MSM_PREPARE_TITLES={titles}",
            "-v", $"\"{archivesTempDir}:/app/data/archives\"",
            "-v", $"\"{serversTempDir}:/app/data/servers\"",
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

        await Task.WhenAll(ProcessStdoutAsync(resultDict, process, validationService, cancellationToken), ProcessStderrAsync(process, cancellationToken));

        await process.WaitForExitAsync(cancellationToken);
    }

    private async Task ProcessStderrAsync(Process process, CancellationToken cancellationToken)
    {
        string? line;
        while ((line = await process.StandardError.ReadLineAsync(cancellationToken)) is not null)
        {
            logger.LogInformation("Validation [stderr]: {Line}", line);
        }
    }

    private async Task ProcessStdoutAsync(IDictionary<Guid, ValidationResultEntity> resultDict, Process process, IValidationService validationService, CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;

        await foreach (var validatePathResult in JsonSerializer.DeserializeAsyncEnumerable<JsonElement>(process.StandardOutput.BaseStream, cancellationToken: cancellationToken))
        {
            var fileName = validatePathResult.GetProperty("FileName").GetString();

            if (string.IsNullOrEmpty(fileName))
            {
                logger.LogWarning("Validation result with empty FileName!");
                continue;
            }

            var resultId = Guid.Parse(GBX.NET.GbxPath.GetFileNameWithoutExtension(fileName));
            
            if (!resultDict.TryGetValue(resultId, out var result))
            {
                logger.LogWarning("Validation result {ResultId} not found in dictionary!", resultId);
                continue;
            }

            logger.LogInformation("Validation result {ResultId}: {Result}", result.Id, validatePathResult);

            result.Status = ValidationStatus.Completed;
            result.RawJsonResult = validatePathResult.ToString();
            result.CompletedAt = DateTimeOffset.UtcNow;

            foreach (var property in validatePathResult.EnumerateObject())
            {
                switch (property.Name)
                {
                    case "FileName":
                        // handled beforehand
                        break;
                    case "IsValid":
                        result.IsValid = property.Value.GetBoolean();
                        break;
                    case "NbCheckpoints":
                        result.DeclaredNbCheckpoints = property.Value.GetInt32();
                        break;
                    case "NbRespawns":
                        result.DeclaredNbRespawns = property.Value.GetInt32();
                        break;
                    case "Time":
                        {
                            var time = (int)property.Value.GetUInt32();
                            result.DeclaredTime = time == -1 ? null : TimeInt32.FromMilliseconds(time);
                        }
                        break;
                    case "Score":
                        result.DeclaredScore = property.Value.GetInt32();
                        break;
                    case "DeclaredResult":
                        foreach (var subProperty in property.Value.EnumerateObject())
                        {
                            switch (subProperty.Name)
                            {
                                case "NbCheckpoints":
                                    result.DeclaredNbCheckpoints = subProperty.Value.GetInt32();
                                    break;
                                case "NbRespawns":
                                    result.DeclaredNbRespawns = subProperty.Value.GetInt32();
                                    break;
                                case "Time":
                                    var time = (int)subProperty.Value.GetUInt32();
                                    result.DeclaredTime = time == -1 ? null : TimeInt32.FromMilliseconds(time);
                                    break;
                                case "Score":
                                    result.DeclaredScore = subProperty.Value.GetInt32();
                                    break;
                            }
                        }
                        break;
                    case "ValidatedResult":
                        foreach (var subProperty in property.Value.EnumerateObject())
                        {
                            switch (subProperty.Name)
                            {
                                case "NbRespawns":
                                    result.ValidatedNbRespawns = subProperty.Value.GetInt32();
                                    break;
                                case "NbCheckpoints":
                                    result.ValidatedNbCheckpoints = subProperty.Value.GetInt32();
                                    break;
                                case "Time":
                                    var time = (int)subProperty.Value.GetUInt32();
                                    result.ValidatedTime = time == -1 ? null : TimeInt32.FromMilliseconds(time);
                                    break;
                                case "Score":
                                    result.ValidatedScore = subProperty.Value.GetInt32();
                                    break;
                            }
                        }
                        break;
                    case "Inputs":
                        result.InputsResult = property.Value.ToString();
                        break;
                    case "AccountId":
                        if (Guid.TryParse(property.Value.GetString(), out var guid))
                        {
                            result.AccountId = guid;
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

            await validationService.FinishProcessingAsync(result, cancellationToken);
        }
    }
}
