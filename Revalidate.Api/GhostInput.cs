using TmEssentials;

namespace Revalidate.Api;

public sealed class GhostInput
{
    public required TimeInt32 Time { get; set; }
    public required string Name { get; set; }
    public int? Value { get; set; }
    public bool? Pressed { get; set; }
    public ushort? X { get; set; }
    public ushort? Y { get; set; }
    public float? ValueF { get; set; }
}
