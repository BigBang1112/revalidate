namespace Revalidate.Models;

public sealed class RevalidateInformation
{
    public string Message => "Welcome to Revalidate!";
    public GitInformation Git { get; } = new();
}
