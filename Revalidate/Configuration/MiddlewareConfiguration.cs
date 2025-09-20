using HealthChecks.UI.Client;
using Scalar.AspNetCore;

namespace Revalidate.Configuration;

public static class MiddlewareConfiguration
{
    public static void UseMiddleware(this WebApplication app)
    {
        app.UseHttpsRedirection();

        if (!app.Environment.IsDevelopment())
        {
            app.UseResponseCompression();
        }

        app.UseCors();

        app.UseRateLimiter();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseOutputCache();

        app.MapHealthChecks("/_health", new()
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        }).RequireAuthorization();

        app.MapOpenApi().CacheOutput();
        app.MapScalarApiReference(options =>
        {
            options.Theme = ScalarTheme.DeepSpace;
        });

        app.MapEndpoints();
    }
}
