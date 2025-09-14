using System.Collections.Immutable;

namespace Revalidate.Api;

public sealed class ValidationRequest
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset? CompletedAt { get; init; }
    public required Dictionary<string, string[]> Warnings { get; init; }
    public required ImmutableList<ValidationResult> Results { get; init; }
}
