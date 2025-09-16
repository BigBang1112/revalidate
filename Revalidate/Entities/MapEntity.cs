using Microsoft.EntityFrameworkCore;
using Revalidate.Api;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TmEssentials;

namespace Revalidate.Entities;

[Index(nameof(MapUid))]
[Index(nameof(Sha256), IsUnique = true)]
public sealed class MapEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    [StringLength(32)]
    public required string MapUid { get; set; }

    [Column(TypeName = "BINARY(32)")]
    public required byte[] Sha256 { get; set; }

    public required GameVersion GameVersion { get; set; }

    [StringLength(byte.MaxValue)]
    public string? Name { get; set; }

    [StringLength(byte.MaxValue)]
    public string? DeformattedName { get; set; }

    [StringLength(16)]
    public string? EnvironmentId { get; set; }

    [StringLength(16)]
    public string? ModeId { get; set; }

    public TimeInt32? AuthorTime { get; set; }

    public int? AuthorScore { get; set; }

    public int NbLaps { get; set; } = 1;

    public required FileEntity File { get; set; }

    [Column(TypeName = "mediumblob")]
    public required byte[]? Thumbnail { get; set; }

    public required bool UserUploaded { get; set; }
}
