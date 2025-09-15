using System.ComponentModel.DataAnnotations;
using TmEssentials;

namespace Revalidate.Entities;

public sealed class GhostInputEntity
{
    public int Id { get; set; }

    public required ValidationResultEntity ValidationResult { get; set; }
    public Guid ValidationResultId { get; set; }

    public required TimeInt32 Time { get; set; }

    [StringLength(24)]
    public required string Name { get; set; }

    public int? Value { get; set; }

    public bool? Pressed { get; set; }

    public ushort? X { get; set; }
    public ushort? Y { get; set; }

    public float? ValueF { get; set; }
}
