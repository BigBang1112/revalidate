using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Revalidate.Api;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped(provider => new HttpClient
{
    BaseAddress = new Uri(builder.Configuration["API:BaseAddress"] ?? throw new InvalidOperationException("API:BaseAddress configuration is missing"))
});

builder.Services.AddScoped(provider =>
{
    var httpClient = provider.GetRequiredService<HttpClient>();
    return new RevalidateClient(httpClient);
});

await builder.Build().RunAsync();
