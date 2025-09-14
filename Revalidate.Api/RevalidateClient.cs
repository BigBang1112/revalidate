namespace Revalidate.Api;

public sealed class RevalidateClient
{
    private readonly HttpClient http;

    public RevalidateClient(HttpClient http)
    {
        this.http = http;
    }

    public RevalidateClient() : this(new HttpClient())
    {
        
    }
}
