namespace Revalidate.Models;

public sealed class GitInformation
{
    public string Branch { get; } = GitInfo.Branch;
    public string Commit { get; } = GitInfo.CommitHash;
    public DateTimeOffset CommitDate { get; } = GitInfo.CommitDate;
    public string? Tag { get; } = GitInfo.Tag;
}
