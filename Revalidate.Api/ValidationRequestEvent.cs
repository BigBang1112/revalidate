namespace Revalidate.Api;

public sealed class ValidationRequestEvent
{
    public ValidationRequest? Request { get; init; }
    public ValidationResult? Result { get; init; }
    public Guid? ResultId { get; init; }
    public string? DistroId { get; init; }
    public string? Message { get; init; }
    public string? Json { get; init; }
}
