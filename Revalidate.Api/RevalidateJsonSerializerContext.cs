using Revalidate.Api.Converters.Json;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Revalidate.Api;

[JsonSerializable(typeof(ValidationRequest))]
[JsonSerializable(typeof(RevalidateInformation))]
[JsonSerializable(typeof(ImmutableList<GhostInput>))]
[JsonSourceGenerationOptions(Converters = [typeof(JsonStringEnumConverter), typeof(JsonTimeInt32Converter)], PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class RevalidateJsonSerializerContext : JsonSerializerContext;