using Microsoft.OpenApi.Models;
using MinimalHelpers.OpenApi;
using Revalidate;
using Revalidate.Extensions;
using Revalidate.Filters;
using Serilog;

var builder = WebApplication.CreateSlimBuilder(args);

var appVersion = typeof(Program).Assembly.GetName().Version ?? new Version(1, 0);

builder.Services.AddEndpoints();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddFormFile();

    options.DocumentFilter<ValidateFirstDocumentFilter>();

    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = appVersion.ToString(3),
        Title = "Revalidate API",
        Contact = new OpenApiContact
        {
            Name = "BigBang1112",
            Url = new Uri("https://bigbang1112.cz")
        },
        License = new OpenApiLicense
        {
            Name = "GPL-3.0",
            Url = new Uri("https://www.gnu.org/licenses/gpl-3.0.txt")
        }
    });
});

builder.Services.AddAntiforgery();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddLogging(builder =>
{
    builder.AddSerilog(dispose: true);
});

builder.Services.AddSingleton(x => new RevalidateInfo(appVersion));

builder.Host.UseSerilog();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: "[{SourceContext} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", $"Trackmania Replay Validation Web API");
        options.InjectStylesheet("css/SwaggerDark.css");
    });
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.UseEndpoints();
app.UseStaticFiles();

app.Run();