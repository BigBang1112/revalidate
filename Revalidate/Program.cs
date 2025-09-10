using GBX.NET;
using GBX.NET.LZO;
using Revalidate.Configuration;

Gbx.LZO = new Lzo();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

// Add services to the container.
builder.Services.AddDomainServices();
builder.Services.AddWebServices(builder.Configuration);
builder.Services.AddCacheServices();
builder.Services.AddTelemetryServices(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseMiddleware();

app.Run();