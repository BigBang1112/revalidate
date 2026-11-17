using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;

namespace Revalidate.Entities;

public sealed class FileEntity
{
    // MySQL MEDIUMBLOB stores at most 2^24 - 1 bytes, including downloaded files.
    public const int MaxDataLength = 16 * 1024 * 1024 - 1;

    public Guid Id { get; set; } = Guid.CreateVersion7();

    [Column(TypeName = "mediumblob")]
    public required byte[] Data { get; set; }

    public DateTimeOffset? LastModifiedAt { get; set; }

    [StringLength(64)]
    public string? Etag { get; set; }

    public static async Task<FileEntity> FromStreamAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (stream.Length > MaxDataLength)
        {
            throw new InvalidDataException($"File size of {stream.Length:N0} bytes exceeds the maximum stored file size of {MaxDataLength:N0} bytes.");
        }

        stream.Position = 0;

        await using var ms = new MemoryStream();

        await stream.CopyToAsync(ms, cancellationToken);

        return await FromStreamAsync(ms, cancellationToken);
    }

    public static async Task<FileEntity> FromStreamAsync(MemoryStream stream, CancellationToken cancellationToken)
    {
        if (stream.Length > MaxDataLength)
        {
            throw new InvalidDataException($"File size of {stream.Length:N0} bytes exceeds the maximum stored file size of {MaxDataLength:N0} bytes.");
        }

        stream.Position = 0;

        return new FileEntity
        {
            Data = stream.ToArray(),
            LastModifiedAt = DateTimeOffset.UtcNow,
            Etag = $"\"{Convert.ToHexStringLower(await MD5.HashDataAsync(stream, cancellationToken))}\""
        };
    }
}
