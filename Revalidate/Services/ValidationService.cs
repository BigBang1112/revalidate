using GBX.NET;
using GBX.NET.Engines.Game;
using OneOf;
using OneOf.Types;
using Revalidate.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Revalidate.Services;

public interface IValidationService
{
    Task<OneOf<ValidationResult, ValidationFailed>> ValidateAsync(IEnumerable<IFormFile> files, CancellationToken cancellationToken);
    Task<List<ValidationResult>> GetValidationsAsync(CancellationToken cancellationToken);
    Task<OneOf<ValidationResult, NotFound>> GetValidationByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> DeleteValidationAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class ValidationService : IValidationService
{
    private readonly ILogger<ValidationService> logger;

    public ValidationService(ILogger<ValidationService> logger)
    {
        this.logger = logger;
    }

    public async Task<OneOf<ValidationResult, ValidationFailed>> ValidateAsync(IEnumerable<IFormFile> files, CancellationToken cancellationToken)
    {
        // if all setup (not actual run validation) of ghosts or replays fails, it becomes validation error

        var processedHashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var errorBag = new ConcurrentDictionary<string, List<string>>();

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

            try
            {
                var node = await Gbx.ParseNodeAsync(stream, cancellationToken: cancellationToken);

                switch (node)
                {
                    case CGameCtnReplayRecord replay:
                        // pick Replays folder, store validation info
                        break;
                    case CGameCtnGhost ghost:
                        // pick Replays folder, store validation info
                        break;
                    case CGameCtnChallenge map:
                        // pick Maps folder
                        break;
                    default:
                        AppendError(errorBag, file, "File is not a valid Replay.Gbx, Ghost.Gbx, or Map.Gbx.");
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to parse file '{FileName}' as Gbx.", file.FileName);
                AppendError(errorBag, file, $"File could not be parsed: {ex.Message}");
                continue;
            }

            stream.Position = 0;

            var hashStartTimestamp = Stopwatch.GetTimestamp();

            var sha256 = await SHA256.HashDataAsync(stream, cancellationToken);
            var hash = Convert.ToHexStringLower(sha256);

            var hashEndTimestamp = Stopwatch.GetElapsedTime(hashStartTimestamp).TotalMilliseconds;

            logger.LogInformation("Computed SHA-256 hash for file '{FileName}' in {ElapsedMilliseconds} ms: {Hash}",
                file.FileName, hashEndTimestamp, hash);

            if (processedHashes.TryGetValue(hash, out var duplicateFileName))
            {
                AppendError(errorBag, file, $"Duplicate file detected: '{duplicateFileName}'. It will be skipped.");
                continue;
            }

            processedHashes[hash] = file.FileName;

            // enqueue a job which will copy files to mania-server-manager with a hash file name when its ready
            stream.Position = 0;

            var destinationPath = Path.Combine("Data", "Revalidate", hash);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

            await using var destinationStream = File.Create(destinationPath);
            await stream.CopyToAsync(destinationStream, cancellationToken);
        }

        return new ValidationResult
        {
            Warnings = errorBag.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray())
        };
    }

    private static List<string> AppendError(ConcurrentDictionary<string, List<string>> errorBag, IFormFile file, string message)
    {
        return errorBag.AddOrUpdate(file.FileName,
            _ => [message],
            (_, list) =>
            {
                list.Add(message);
                return list;
            });
    }

    public Task<List<ValidationResult>> GetValidationsAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<OneOf<ValidationResult, NotFound>> GetValidationByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteValidationAsync(Guid id, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}