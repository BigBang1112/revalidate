using Revalidate.Api;
using Revalidate.Entities;

namespace Revalidate.Mapping;

public static class GhostInputEntityMappingExtensions
{
    public static GhostInput ToDto(this GhostInputEntity input) => new()
    {
        Time = input.Time,
        Name = input.Name,
        Value = input.Value,
        Pressed = input.Pressed,
        X = input.X,
        Y = input.Y,
        ValueF = input.ValueF,
    };
}
