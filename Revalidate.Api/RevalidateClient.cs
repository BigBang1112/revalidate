using System.Collections.Immutable;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text.Json;

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

        await EnsureSuccessStatusCodeAsync(response, cancellationToken);

        return (await response.Content.ReadFromJsonAsync(RevalidateJsonSerializerContext.Default.ValidationRequest, cancellationToken))!;
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

    public async Task<RevalidateInformation> GetInfoAsync(CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync("", cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync(RevalidateJsonSerializerContext.Default.RevalidateInformation, cancellationToken) ?? throw new InvalidOperationException("Response content is null");
    }

    public async Task<ValidationRequest?> GetRequestByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync($"/validations/{id}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessStatusCodeAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync(RevalidateJsonSerializerContext.Default.ValidationRequest, cancellationToken);
    }

    public async Task<JsonElement?> GetDistroJsonResultAsync(Guid resultId, string distroId, CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync($"/results/{resultId}/distros/{distroId}/json", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        return JsonElement.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public async Task<string?> GetDistroLogsAsync(Guid resultId, string distroId, CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync($"/results/{resultId}/distros/{distroId}/logs", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }


    public async Task<ImmutableList<GhostInput>> GetResultInputsAsync(Guid resultId, CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync($"/results/{resultId}/inputs", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return [];
        }

        await EnsureSuccessStatusCodeAsync(response, cancellationToken);

        var inputs = await response.Content.ReadFromJsonAsync(RevalidateJsonSerializerContext.Default.ImmutableListGhostInput, cancellationToken);

        return inputs ?? ImmutableList<GhostInput>.Empty;
    }

    public async IAsyncEnumerable<SseItem<ValidationRequestEvent?>> GetRequestEventsAsync(Guid requestId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var response = await client.GetStreamAsync($"/validations/{requestId}/events", cancellationToken);

        var sseParser = SseParser.Create(response, (eventType, bytes) => JsonSerializer.Deserialize(bytes, RevalidateJsonSerializerContext.Default.ValidationRequestEvent));

        await foreach (var e in sseParser.EnumerateAsync(cancellationToken))
        {
            yield return e;
        }
    }

    private static async Task EnsureSuccessStatusCodeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex) when (response.StatusCode == HttpStatusCode.BadRequest)
        {
            try
            {
                var problemDetails = (await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: cancellationToken))!;
                throw new RevalidateProblemException(problemDetails, ex);
            }
            catch
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new RevalidateProblemException(content, ex);
            }
        }
    }
}
