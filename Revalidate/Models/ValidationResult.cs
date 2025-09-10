namespace Revalidate.Models;

public sealed class ValidationResult
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Dictionary<string, string[]> Warnings { get; init; } = [];
}