namespace Revalidate.Api;

public sealed class ValidationDistroResult
{
    public required Guid Id { get; init; }
    public required string DistroId { get; init; }
    public required ValidationStatus Status { get; set; }
    public required bool? IsValid { get; init; }
    public required bool? IsValidExtracted { get; init; }
    public required DateTimeOffset? StartedAt { get; set; }
    public required DateTimeOffset? EndedAt { get; set; }
    public required DateTimeOffset? CompletedAt { get; set; }
    public required string? Desc { get; init; }
    public required ValidationRaceResult? DeclaredResult { get; init; }
    public required ValidationRaceResult? ValidatedResult { get; init; }
    public required Guid? AccountId { get; init; }
    public required string? InputsResult { get; init; }
}
