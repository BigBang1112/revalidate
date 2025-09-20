namespace Revalidate.Api;

public sealed class ValidationDistroResult
{
    public required Guid Id { get; init; }
    public required string DistroId { get; init; }
    public required ValidationStatus Status { get; set; }
    public bool? IsValid { get; init; }
    public bool? IsValidExtracted { get; init; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? Desc { get; init; }
    public ValidationRaceResult? DeclaredResult { get; init; }
    public ValidationRaceResult? ValidatedResult { get; init; }
    public Guid? AccountId { get; init; }
    public string? InputsResult { get; init; }
}
