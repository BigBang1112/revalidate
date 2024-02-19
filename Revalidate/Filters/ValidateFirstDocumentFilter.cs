using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Revalidate.Filters;

public class ValidateFirstDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var reorderedPaths = new OpenApiPaths();

        foreach (var path in swaggerDoc.Paths.OrderBy(e => e.Key))
        {
            if (path.Key.StartsWith("/validate"))
            {
                reorderedPaths.Add(path.Key, path.Value);
            }
        }

        foreach (var path in swaggerDoc.Paths.OrderBy(e => e.Key))
        {
            if (!path.Key.StartsWith("/validate"))
            {
                reorderedPaths.Add(path.Key, path.Value);
            }
        }

        swaggerDoc.Paths = reorderedPaths;
    }
}
