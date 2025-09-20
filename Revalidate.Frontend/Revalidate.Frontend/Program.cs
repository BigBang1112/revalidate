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

var app = builder.Build();

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
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Revalidate.Frontend.Client._Imports).Assembly);

app.Run();
