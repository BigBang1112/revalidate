namespace Revalidate.Exceptions;

[Serializable]
public class MapUidException : Exception
{
	public MapUidException() { }
	public MapUidException(string message) : base(message) { }
	public MapUidException(string message, Exception inner) : base(message, inner) { }
}