namespace Revalidate.Api;

[Serializable]
public class RevalidateRateLimitException : Exception
{
	public RevalidateRateLimitException() { }
	public RevalidateRateLimitException(string message) : base(message) { }
	public RevalidateRateLimitException(string message, Exception inner) : base(message, inner) { }
}
