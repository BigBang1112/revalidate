using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Revalidate.Api;

public sealed class RevalidateClient
{
    private readonly HttpClient client;

    public RevalidateClient(HttpClient client)
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));
        this.client.DefaultRequestHeaders.UserAgent.ParseAdd("Revalidate.Api/1.0 (Discord=bigbang1112)");
    }

    public RevalidateClient(string baseAddress = "https://api.revalidate.gbx.tools") : this(new HttpClient { BaseAddress = new Uri(baseAddress) })
    {
        
    }

    public async Task<ValidationRequest> ValidateAsync(MultipartFormDataContent content, CancellationToken cancellationToken)
    {
        using var response = await client.PostAsync("/validations", content, cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ValidationRequest>(cancellationToken) ?? throw new InvalidOperationException("Response content is null");
    }

    public async Task<ValidationRequest> ValidateAsync(IEnumerable<string> filePaths, CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();

        foreach (var filePath in filePaths)
        {
            AddFormStreamContent(content, Path.GetFileName(filePath), File.OpenRead(filePath));
        }

        return await ValidateAsync(content, cancellationToken);
    }

    public async Task<ValidationRequest> ValidateAsync(IEnumerable<FileStream> fileStreams, CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();

        foreach (var fileStream in fileStreams)
        {
            AddFormStreamContent(content, fileStream.Name, fileStream);
        }

        return await ValidateAsync(content, cancellationToken);
    }

    public async Task<ValidationRequest> ValidateAsync(IEnumerable<(string fileName, Stream stream)> files, CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();

        foreach (var (fileName, stream) in files)
        {
            AddFormStreamContent(content, fileName, stream);
        }

        return await ValidateAsync(content, cancellationToken);
    }

    public async Task<ValidationRequest> ValidateAsync(string filePath, CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();

        AddFormStreamContent(content, Path.GetFileName(filePath), File.OpenRead(filePath));

        return await ValidateAsync(content, cancellationToken);
    }

    public async Task<ValidationRequest> ValidateAsync(FileStream fileStream, CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();

        AddFormStreamContent(content, fileStream.Name, fileStream);

        return await ValidateAsync(content, cancellationToken);
    }

    public async Task<ValidationRequest> ValidateAsync(string fileName, Stream stream, CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();

        AddFormStreamContent(content, fileName, stream);

        return await ValidateAsync(content, cancellationToken);
    }

    private static void AddFormStreamContent(MultipartFormDataContent content, string fileName, Stream stream)
    {
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "files", fileName);
    }
}
