using System.Text;

namespace Revalidate.Api;

public class RevalidateProblemException : Exception
{
    public ProblemDetails? Problem { get; }

    public RevalidateProblemException(ProblemDetails problem, HttpRequestException innerException)
        : base($"{problem.Title}{ConcatErrors(problem.Errors)}", innerException)
    {
        Problem = problem;
    }

    public RevalidateProblemException(string message, HttpRequestException innerException)
        : base(message, innerException)
    {
        
    }

    private static string ConcatErrors(Dictionary<string, string[]> errors)
    {
        if (errors is null or { Count: 0 })
        {
            return string.Empty;
        }

        var sb = new StringBuilder();

        foreach (var kvp in errors)
        {
            sb.Append($"\n{kvp.Key}: {string.Join(", ", kvp.Value)}");
        }

        return sb.ToString();
    }
}
