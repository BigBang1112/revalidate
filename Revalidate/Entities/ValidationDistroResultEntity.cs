using Revalidate.Api;
using System.ComponentModel.DataAnnotations;
using TmEssentials;

namespace Revalidate.Entities;

public class ValidationDistroResultEntity
{
    public int Id { get; set; }

    public ValidationResultEntity? Result { get; set; }
    public Guid? ResultId { get; set; }

    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    [StringLength(16)]
    public required string DistroId { get; set; }

    public ValidationStatus Status { get; set; }

    public bool? IsValid { get; set; }
    public bool? IsValidExtracted { get; set; }

    public int? DeclaredNbCheckpoints { get; set; }
    public int? DeclaredNbRespawns { get; set; }
    public TimeInt32? DeclaredTime { get; set; }
    public int? DeclaredScore { get; set; }

    public int? ValidatedNbCheckpoints { get; set; }
    public int? ValidatedNbRespawns { get; set; }
    public TimeInt32? ValidatedTime { get; set; }
    public int? ValidatedScore { get; set; }

    public Guid? AccountId { get; set; }

    [StringLength(short.MaxValue)]
    public string? InputsResult { get; set; }

    [StringLength(byte.MaxValue)]
    public string? Desc { get; set; }

    [StringLength(short.MaxValue)]
    public string? RawJsonResult { get; set; }

    public ValidationLogEntity? Log { get; set; }
}
