using Revalidate.Frontend.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthenticationServices(builder.Environment);
builder.Services.AddWebServices(builder.Configuration);
builder.Services.AddTelemetryServices(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseMiddleware();

app.Run();
