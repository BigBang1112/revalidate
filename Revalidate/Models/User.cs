namespace Revalidate.Models;

public sealed class User
{
    public required string Login { get; set; }
    public required string HtmlUrl { get; set; }
    public required string Blog { get; set; }
}
