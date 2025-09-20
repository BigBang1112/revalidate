namespace Revalidate.Api;

public sealed class GitInformation
{
    public required string Branch { get; init; }
    public required string Commit { get; init; }
    public required DateTimeOffset CommitDate { get; init; }
    public string? Tag { get; init; }
}
