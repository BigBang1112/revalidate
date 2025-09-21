using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Revalidate.Api;
using Revalidate.Frontend.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents().AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10 MB
    })
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddHttpClient()
    .ConfigureHttpClientDefaults(httpBuilder =>
    {
        httpBuilder.ConfigureHttpClient(client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["API:BaseAddress"] ?? throw new InvalidOperationException("API:BaseAddress configuration is missing"));
            client.DefaultRequestHeaders.UserAgent.ParseAdd("RevalidateFrontend/1.0 (Remote Replay Validation Frontend; Discord=bigbang1112)");
        });
    });

builder.Services.AddScoped(provider =>
{
    var httpClient = provider.GetRequiredService<HttpClient>();
    return new RevalidateClient(httpClient);
});

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

if (!app.Environment.IsDevelopment())
{
    app.UseResponseCompression();
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Revalidate.Frontend.Client._Imports).Assembly);

app.Run();
