using Revalidate.Api;
using Revalidate.Entities;
using System.Collections.Immutable;

namespace Revalidate.Mapping;

public static class ValidationRequestEntityMappingExtensions
{
    public static ValidationRequest ToDto(this ValidationRequestEntity request) => new()
    {
        Id = request.Id,
        CreatedAt = request.CreatedAt,
        CompletedAt = request.CompletedAt,
        Warnings = request.Warnings,
        Results = request.Results.Select(x => x.ToDto()).ToImmutableList()
    };
}
