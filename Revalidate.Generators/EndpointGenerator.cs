using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace Revalidate.Generators;

[Generator(LanguageNames.CSharp)]
public class EndpointGenerator : IIncrementalGenerator
{
    private const bool Debug = false;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        if (Debug && !Debugger.IsAttached)
        {
            Debugger.Launch();
        }

        var values = context.CompilationProvider.Select(ProvideEndpointInfo);

        context.RegisterSourceOutput(values, GenerateSource);
    }

    private IEnumerable<INamespaceSymbol> RecurseNamespaces(INamespaceSymbol namespaceSymbol)
    {
        yield return namespaceSymbol;

        foreach (var n in namespaceSymbol.GetNamespaceMembers())
        {
            foreach (var nn in RecurseNamespaces(n))
            {
                yield return nn;
            }
        }
    }

    private ImmutableArray<INamedTypeSymbol> ProvideEndpointInfo(Compilation compilation, CancellationToken token)
    {
        var endpoints = ImmutableArray.CreateBuilder<INamedTypeSymbol>();

        foreach (var typeSymbol in RecurseNamespaces(compilation.GlobalNamespace).SelectMany(x => x.GetTypeMembers()))
        {
            if (typeSymbol.AllInterfaces.Any(x => x.Name == "IEndpoint"))
            {
                endpoints.Add(typeSymbol);
            }
        }

        return endpoints.ToImmutable();
    }

    private void GenerateSource(SourceProductionContext context, ImmutableArray<INamedTypeSymbol> endpoints)
    {
        var sb = new StringBuilder();
        sb.AppendLine("namespace Revalidate.Extensions;");
        sb.AppendLine();
        sb.AppendLine("internal static partial class EndpointServiceExtensions");
        sb.AppendLine("{");
        sb.AppendLine("    public static partial IServiceCollection AddEndpoints(this IServiceCollection services)");
        sb.AppendLine("    {");

        foreach (var endpoint in endpoints)
        {
            sb.Append("        ");
            sb.Append("services.AddSingleton<IEndpoint, ");
            sb.Append(endpoint);
            sb.AppendLine(">();");
        }

        sb.AppendLine("        return services;");

        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource("EndpointServiceExtensions", sb.ToString());
    }
}