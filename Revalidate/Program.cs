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
builder.Services.AddWebServices(builder.Configuration, builder.Environment);
builder.Services.AddDataServices(builder.Configuration);
builder.Services.AddCacheServices();
builder.Services.AddTelemetryServices(builder.Configuration, builder.Environment);

var app = builder.Build();

if (builder.Environment.IsDevelopment())
{
    app.MigrateDatabase();
}

app.UseMiddleware();

app.Run();