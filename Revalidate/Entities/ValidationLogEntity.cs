namespace Revalidate.Entities;

public sealed class ValidationLogEntity
{
    public int Id { get; init; }
    public required string Log { get; set; }

    public List<ValidationDistroResultEntity> Results { get; set; } = [];
}
