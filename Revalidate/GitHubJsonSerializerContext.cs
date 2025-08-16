using Revalidate.Models;
using System.Text.Json.Serialization;

namespace Revalidate;

[JsonSerializable(typeof(User))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
internal partial class GitHubJsonSerializerContext : JsonSerializerContext;