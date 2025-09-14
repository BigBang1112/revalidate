namespace Revalidate.Entities;

public sealed class ValidationRequestEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public required DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public Dictionary<string, string[]> Warnings { get; set; } = [];

    public List<ValidationResultEntity> Results { get; set; } = [];
}
