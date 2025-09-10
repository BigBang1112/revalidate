namespace Revalidate.Models;

public sealed record ValidationFailed(Dictionary<string, string[]> Errors);